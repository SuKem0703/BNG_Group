using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(BoxCollider2D))]
public class ManaProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float lifeTime = 3f;
    public float defaultProjectileSpeed = 15f;
    public float hitAnimationDuration = 0.4f;

    [Header("Collision Settings")]
    public LayerMask targetLayer;

    // Layer Decor dùng cho Tilemap blocker, không cần thêm vào targetLayer.
    private int decorLayer = -1;

    private int damage;
    private bool isCritical;
    private ulong ownerClientId;

    private Rigidbody2D rb;
    private Animator animator;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    private bool hasHit = false;
    private bool inUse = false;

    // Pool chỉ được lấy lại projectile đã kết thúc vòng đời.
    public bool IsAvailableForPool => !inUse;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        decorLayer = LayerMask.NameToLayer("Decor");

        if (decorLayer < 0)
            Debug.LogWarning("[ManaProjectile] Chưa có Layer Decor. Đạn vẫn hoạt động với Enemy.", this);

        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
    }

    public void Initialize(Vector2 direction, float speed, int dmg, bool crit, ulong clientId)
    {
        // Enforce four-way projectile travel even if a caller supplies a diagonal vector.
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.down;
        }
        else if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            direction = direction.x >= 0f ? Vector2.right : Vector2.left;
        }
        else
        {
            direction = direction.y >= 0f ? Vector2.up : Vector2.down;
        }

        damage = dmg;
        isCritical = crit;
        ownerClientId = clientId;
        hasHit = false;
        inUse = true;

        ToggleVisualAndNetworkClientRpc(true);

        // THÊM ĐOẠN RESET ANIMATOR TẠI ĐÂY
        if (animator != null)
        {
            animator.ResetTrigger("Hit"); // Xóa sạch trigger Hit cũ nếu còn lưu
            animator.Rebind();            // Đưa Animator về trạng thái ban đầu của clip bay
            animator.Update(0f);          // Cập nhật ngay lập tức frame đầu tiên

            animator.SetFloat("LastInputX", direction.x);
            animator.SetFloat("LastInputY", direction.y);
        }

        float finalSpeed = speed > 0 ? speed : defaultProjectileSpeed;
        rb.linearVelocity = direction * finalSpeed;

        CancelInvoke(nameof(DeactivateProjectile));
        Invoke(nameof(DeactivateProjectile), lifeTime);
    }

    // Keep pooled NetworkObjects active. Deactivating the GameObject after Spawn
    // can prevent the object/RPC lifecycle from being observed by remote clients.
    public void DeactivateForPool()
    {
        if (!IsServer) return;

        inUse = false;
        hasHit = true;
        CancelInvoke(nameof(DeactivateProjectile));
        rb.linearVelocity = Vector2.zero;

        if (boxCollider != null) boxCollider.enabled = false;
        ToggleVisualAndNetworkClientRpc(false);
    }

    public void InitializeAndActivate(Vector2 direction, float speed, int dmg, bool crit, ulong clientId)
    {
        Initialize(direction, speed, dmg, crit, clientId);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || !inUse || hasHit || collision == null) return;

        // Decor: chỉ Tilemap blocker (không phải Trigger) mới làm đạn nổ.
        // Decor Trigger luôn cho phép đạn đi xuyên, kể cả khi có trong targetLayer.
        if (collision.gameObject.layer == decorLayer)
        {
            if (collision.isTrigger || !IsDecorTilemapCollider(collision))
                return;

            BeginImpact(null, null); // Nổ tại tường, KHÔNG gây damage cho Enemy.
            return;
        }

        // Giữ nguyên logic va chạm các layer mục tiêu đã cấu hình.
        if ((targetLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Enemy enemy = collision.GetComponentInParent<Enemy>();
        Transform attackerTransform = null;

        if (enemy != null && !enemy.IsDead && enemy.netHealth.Value > 0)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var client) &&
                client.PlayerObject != null)
            {
                attackerTransform = client.PlayerObject.transform;
            }

            BeginImpact(enemy, attackerTransform);
        }
        else
        {
            BeginImpact(null, null);
        }
    }

    private static bool IsDecorTilemapCollider(Collider2D collider)
    {
        // Khi Tilemap dùng Composite, collider vật lý nhận được sẽ là CompositeCollider2D.
        return collider is TilemapCollider2D ||
               (collider is CompositeCollider2D &&
                collider.GetComponent<TilemapCollider2D>() != null);
    }

    private void BeginImpact(Enemy enemy, Transform attackerTransform)
    {
        hasHit = true;
        CancelInvoke(nameof(DeactivateProjectile));
        StartCoroutine(HandleHitSequence(enemy, attackerTransform));
    }

    private IEnumerator HandleHitSequence(Enemy enemy, Transform attackerTransform)
    {
        rb.linearVelocity = Vector2.zero;
        if (boxCollider != null) boxCollider.enabled = false;

        PlayHitAnimationClientRpc();

        if (enemy != null)
        {
            enemy.TakeDamage(damage, DamageSourceType.Mage, attackerTransform, isCritical, false);
        }

        yield return new WaitForSeconds(hitAnimationDuration);

        DeactivateProjectile();
    }

    [ClientRpc]
    private void PlayHitAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void DeactivateProjectile()
    {
        if (!IsServer || !inUse) return;
        inUse = false;
        hasHit = true;
        CancelInvoke(nameof(DeactivateProjectile));

        rb.linearVelocity = Vector2.zero;
        if (boxCollider != null) boxCollider.enabled = false;

        ToggleVisualAndNetworkClientRpc(false);
    }

    [ClientRpc]
    private void ToggleVisualAndNetworkClientRpc(bool isActive)
    {
        if (isActive)
        {
            gameObject.SetActive(true);
            if (boxCollider != null) boxCollider.enabled = true;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;
        }
    }
}
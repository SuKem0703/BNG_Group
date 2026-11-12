using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(BoxCollider2D))]
public class ManaProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float lifeTime = 3f;
    public float defaultProjectileSpeed = 15f;
    public float hitAnimationDuration = 0.4f;

    [Header("Collision Settings")]
    public LayerMask targetLayer;

    private int damage;
    private bool isCritical;
    private ulong ownerClientId;

    private Rigidbody2D rb;
    private Animator animator;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

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
        hasHit = true;
        CancelInvoke(nameof(DeactivateProjectile));
        rb.linearVelocity = Vector2.zero;

        if (boxCollider != null) boxCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    public void InitializeAndActivate(Vector2 direction, float speed, int dmg, bool crit, ulong clientId)
    {
        Initialize(direction, speed, dmg, crit, clientId);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || hasHit) return;

        if ((targetLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        hasHit = true;
        CancelInvoke(nameof(DeactivateProjectile));

        Enemy enemy = collision.GetComponentInParent<Enemy>();

        if (enemy != null && !enemy.IsDead && enemy.netHealth.Value > 0)
        {
            Transform attackerTransform = null;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var client))
            {
                attackerTransform = client.PlayerObject.transform;
            }

            StartCoroutine(HandleHitSequence(enemy, attackerTransform));
        }
        else
        {
            StartCoroutine(HandleHitSequence(null, null));
        }
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
        if (!IsServer || (hasHit && rb.linearVelocity == Vector2.zero && !gameObject.activeSelf)) return;
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
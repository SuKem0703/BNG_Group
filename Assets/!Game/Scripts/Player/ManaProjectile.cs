using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class ManaProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float lifeTime = 3f;

    [Tooltip("Thời gian tồn tại thêm để chạy hết Animation nổ (Frame 3 đến cuối)")]
    public float hitAnimationDuration = 0.4f;

    private int damage;
    private bool isCritical;
    private ulong ownerClientId;

    private Rigidbody2D rb;
    private Animator animator;

    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public void Initialize(Vector2 direction, float speed, int dmg, bool crit, ulong clientId)
    {
        damage = dmg;
        isCritical = crit;
        ownerClientId = clientId;
        hasHit = false;

        if (animator != null)
        {
            animator.SetFloat("LastInputX", direction.x);
            animator.SetFloat("LastInputY", direction.y);
        }

        rb.linearVelocity = direction * speed;

        Invoke(nameof(DespawnProjectile), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || hasHit) return;

        if (collision.CompareTag("Player") || collision.CompareTag("PlayerController")) return;

        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null && !enemy.IsDead && enemy.netHealth.Value > 0)
        {
            hasHit = true;
            CancelInvoke(nameof(DespawnProjectile));

            Transform attackerTransform = null;
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var client))
            {
                attackerTransform = client.PlayerObject.transform;
            }

            StartCoroutine(HandleHitSequence(enemy, attackerTransform));
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            hasHit = true;
            CancelInvoke(nameof(DespawnProjectile));

            StartCoroutine(HandleHitSequence(null, null));
        }
    }

    private IEnumerator HandleHitSequence(Enemy enemy, Transform attackerTransform)
    {
        rb.linearVelocity = Vector2.zero;

        PlayHitAnimationClientRpc();

        if (enemy != null)
        {
            enemy.TakeDamage(damage, DamageSourceType.Mage, attackerTransform, isCritical, false);
        }

        yield return new WaitForSeconds(hitAnimationDuration);

        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [ClientRpc]
    private void PlayHitAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void DespawnProjectile()
    {
        if (!IsSpawned || hasHit) return;
        GetComponent<NetworkObject>().Despawn(true);
    }
}
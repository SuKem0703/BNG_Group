using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private Enemy enemy;
    private Coroutine hurtCoroutine;

    public void Init(Enemy mainScript)
    {
        enemy = mainScript;
    }

    public void ProcessDamage(int rawDamage, DamageSourceType damageSourceType, Transform attacker = null, bool isCritical = false, bool forceKnockback = false)
    {
        if (!enemy.IsServer || enemy.isDead || enemy.isTransitioning) return;

        float reductionMultiplier = 100f / (enemy.defense + 100f);
        int finalDamage = Mathf.Max(Mathf.CeilToInt(rawDamage * reductionMultiplier), 1);

        enemy.netHealth.Value -= finalDamage;

        enemy.TakeDamageVisualsClientRpc(finalDamage, damageSourceType, isCritical);

        if (enemy.netHealth.Value <= 0 && !enemy.isDead)
        {
            enemy.isAttacking = false;
            enemy.isStunned = false;
            enemy.isKnockedBack = false;
            StopHurt();

            enemy.HandleHealthDepleted();
            return;
        }

        if (enemy.netHealth.Value > 0)
        {
            StopHurt();

            enemy.isStunned = false;
            bool shouldStun = isCritical || forceKnockback;

            if (!shouldStun && attacker != null)
            {
                var pStats = attacker.GetComponentInParent<PlayerStats>();
                if (pStats != null && pStats.level > enemy.levelEnemy + 5 && isCritical)
                {
                    shouldStun = true;
                }
            }

            if (attacker != null && enemy.enemyRank != EnemyRank.Boss)
            {
                ApplyKnockback(attacker);
            }

            if (shouldStun)
            {
                if (enemy.isAttacking) enemy.isAttacking = false;
                hurtCoroutine = StartCoroutine(HurtRoutine());
            }
        }
    }

    public void ApplyKnockback(Transform attackerTransform)
    {
        if (enemy.isDead || enemy.isTransitioning) return;
        StartCoroutine(KnockbackRoutine(attackerTransform));
    }

    private IEnumerator KnockbackRoutine(Transform attackerTransform)
    {
        enemy.isKnockedBack = true;
        Vector2 direction = (transform.position - attackerTransform.position).normalized;
        enemy.rb.linearVelocity = direction * enemy.knockbackForce;

        yield return new WaitForSeconds(enemy.knockbackDuration);

        if (!enemy.isDead) enemy.rb.linearVelocity = Vector2.zero;
        enemy.isKnockedBack = false;
    }

    private IEnumerator HurtRoutine()
    {
        enemy.isStunned = true;
        enemy.isAttacking = false;
        if (!enemy.isKnockedBack) enemy.rb.linearVelocity = Vector2.zero;
        enemy.netIsWalking.Value = false;

        enemy.TriggerHurtClientRpc();

        yield return new WaitForSeconds(enemy.hurtDuration);

        enemy.isStunned = false;
        hurtCoroutine = null;
    }

    public void StopHurt()
    {
        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
    }
}
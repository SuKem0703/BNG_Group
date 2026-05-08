using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    private Enemy enemy;
    public Transform player { get; private set; }
    private List<Transform> playersInRange = new List<Transform>();
    private bool isInBattleState = false;

    private PlayerStats currentAggroTarget;

    public void Init(Enemy mainScript)
    {
        enemy = mainScript;
    }

    public void OnPlayerDetected(Transform detectedPlayer)
    {
        if (!enemy.IsServer) return;
        if (!playersInRange.Contains(detectedPlayer)) playersInRange.Add(detectedPlayer);
    }

    public void OnPlayerLost(Transform lostPlayer)
    {
        if (!enemy.IsServer) return;
        if (playersInRange.Contains(lostPlayer)) playersInRange.Remove(lostPlayer);

        if (playersInRange.Count == 0)
        {
            player = null;
            SetBattleState(false);
            StopMovement();
        }
    }

    private void UpdateTarget()
    {
        playersInRange.RemoveAll(p => p == null);

        float minDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (var p in playersInRange)
        {
            float dist = Vector2.Distance(transform.position, p.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = p;
            }
        }

        player = closest;
    }

    private void SetBattleState(bool state, PlayerStats targetStats = null)
    {
        if (state)
        {
            if (targetStats == null) return;

            if (currentAggroTarget != null && currentAggroTarget != targetStats)
            {
                currentAggroTarget.ChangeAggro(-1);
            }

            if (currentAggroTarget != targetStats)
            {
                currentAggroTarget = targetStats;
                currentAggroTarget.ChangeAggro(1);
            }
        }
        else
        {
            if (currentAggroTarget != null)
            {
                currentAggroTarget.ChangeAggro(-1);
                currentAggroTarget = null;
            }
        }
        isInBattleState = state;
    }

    public void OnUpdate()
    {
        if (!enemy.IsServer) return;

        UpdateTarget();

        PlayerStats targetStats = player != null ? player.GetComponentInParent<PlayerStats>() : null;

        if (enemy.isDead || enemy.isTransitioning || enemy.netHealth.Value <= 0 || player == null || targetStats == null)
        {
            SetBattleState(false);
            StopMovement();
            return;
        }

        if (enemy.isAttacking || enemy.isStunned) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= enemy.detectionRadius)
        {
            SetBattleState(true, targetStats); // [Cập nhật]
        }
        else
        {
            SetBattleState(false);
        }

        if (distanceToPlayer <= enemy.attackRange - enemy.attackTriggerBuffer)
        {
            StopMovement();
            enemy.netDirection.Value = (player.position - transform.position).normalized;

            if (Time.time >= enemy.lastAttackTime + enemy.attackCooldown)
                PerformAttack();
        }
        else
        {
            ChasePlayer();
        }
    }

    private void OnDisable()
    {
        SetBattleState(false);
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        enemy.rb.linearVelocity = direction * enemy.chaseSpeed;
        enemy.netDirection.Value = direction;
        enemy.netIsWalking.Value = true;
    }

    public void StopMovement()
    {
        if (enemy.rb != null) enemy.rb.linearVelocity = Vector2.zero;
        enemy.netIsWalking.Value = false;
    }

    private void PerformAttack()
    {
        enemy.isAttacking = true;
        enemy.hasDealtDamageThisAttack = false;
        StopMovement();

        enemy.PerformAttackClientRpc(enemy.netDirection.Value);
    }

    public void ProcessDealDamage()
    {
        if (!enemy.IsServer || enemy.isDead || enemy.isStunned || enemy.hasDealtDamageThisAttack || PauseController.IsGamePause) return;

        if (player != null && Vector2.Distance(transform.position, player.position) <= enemy.attackRange)
        {
            var health = player.GetComponentInParent<PlayerStats>();
            if (health != null && !health.isInvincible && !health.IsProcessingDeath)
            {
                health.TakeDamage((int)enemy.damage);
                enemy.hasDealtDamageThisAttack = true;
            }
        }
    }

    public void ProcessEndAttack()
    {
        if (!enemy.IsServer) return;

        enemy.isAttacking = false;
        enemy.lastAttackTime = Time.time;

        if (enemy.netHealth.Value <= 0 && !enemy.isDead)
        {
            enemy.isDead = true;
            enemy.Die();
        }
    }
}
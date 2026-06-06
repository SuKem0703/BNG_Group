using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyCombatAI : MonoBehaviour
{
    private Enemy enemy;
    public Transform player { get; private set; }
    private List<Transform> playersInRange = new List<Transform>();
    private bool isInBattleState = false;

    [SerializeField] private NavMeshAgent agent;
    private PlayerStats currentAggroTarget;

    private bool hasCalledForHelp = false;

    public void Init(Enemy mainScript)
    {
        enemy = mainScript;
        
        if (agent != null)
        {
            agent.updateRotation = false; 
            agent.updateUpAxis = false;   
        }
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

            if (!hasCalledForHelp && player != null)
            {
                CallForHelp(player);
                hasCalledForHelp = true;
            }
        }
        else
        {
            if (currentAggroTarget != null)
            {
                currentAggroTarget.ChangeAggro(-1);
                currentAggroTarget = null;
            }
            
            hasCalledForHelp = false; 
        }
        isInBattleState = state;
    }

    private void CallForHelp(Transform targetPlayer)
    {
        if (enemy.parentArea != null)
        {
            enemy.parentArea.AlertEcosystem(targetPlayer, enemy);
        }
    }

    public void OnUpdate()
    {
        if (!enemy.IsServer) return;

        if (agent != null && agent.isActiveAndEnabled && !agent.isOnNavMesh)
        {
            // agent.Warp(transform.position); 
            return; 
        }

        if (agent != null) agent.speed = enemy.chaseSpeed;

        UpdateTarget();

        PlayerStats targetStats = player != null ? player.GetComponentInParent<PlayerStats>() : null;

        if (enemy.isDead || enemy.isTransitioning || enemy.netHealth.Value <= 0 || player == null || targetStats == null)
        {
            SetBattleState(false);
            StopMovement();
            return;
        }

        if (enemy.isAttacking || enemy.isStunned) 
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float chaseRadius = enemy.detectionRadius * 1.5f;

        if (distanceToPlayer > chaseRadius)
        {
            OnPlayerLost(player);
            return;
        }

        SetBattleState(true, targetStats); 

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
        hasCalledForHelp = false;
    }

private void ChasePlayer()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            Vector2 direction = (agent.steeringTarget - transform.position).normalized;
            if (direction != Vector2.zero)
            {
                enemy.netDirection.Value = direction;
            }
        }
        
        enemy.netIsWalking.Value = true;
    }

public void StopMovement()
{
    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }
    else if (agent != null && !agent.isOnNavMesh)
    {
        // Debug.LogWarning("Agent chưa trên NavMesh!");
    }

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
            var health = player.GetComponentInParent<PlayerVitals>();
            if (health != null && !health.isInvincible && !health.isProcessingDeath)
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
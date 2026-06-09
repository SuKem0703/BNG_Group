using System.Collections;
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

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;

    private Vector2 patrolTarget;
    private float patrolWaitTimer;
    private bool isWaitingAtPatrolPoint = true;
    public bool IsReturningHome { get; private set; }
    private Vector2 homePos;

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
        
        if (IsReturningHome) return; 

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

    public void StartReturnHome(Vector2 origin)
    {
        IsReturningHome = true;
        player = null;
        playersInRange.Clear();
        SetBattleState(false);
        homePos = origin;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(homePos);
        }
        
        enemy.netIsChasing.Value = false;
        enemy.netIsWalking.Value = true;
    }

    public void OnProvoked(Transform attacker)
    {
        if (!enemy.IsServer) return;

        if (IsReturningHome)
        {
            IsReturningHome = false;
        }

        if (attacker != null && !playersInRange.Contains(attacker))
        {
            playersInRange.Add(attacker);
        }
    }

    public void OnUpdate()
    {
        if (!enemy.IsServer) return;
        if (agent != null && agent.isActiveAndEnabled && !agent.isOnNavMesh) return;

        if (enemy.isDead || enemy.isTransitioning || enemy.netHealth.Value <= 0)
        {
            SetBattleState(false);
            StopMovement();
            return;
        }

        if (enemy.isAttacking || enemy.isStunned || enemy.isKnockedBack) 
        {
            StopMovement();
            return;
        }

        if (IsReturningHome)
        {
            if (agent != null)
            {
                agent.speed = patrolSpeed;
                Vector2 moveDir = agent.velocity.normalized;
                if (moveDir.sqrMagnitude > 0.01f) enemy.netDirection.Value = moveDir;

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    IsReturningHome = false;
                    StopMovement();
                    
                    isWaitingAtPatrolPoint = true;
                    patrolWaitTimer = Random.Range(minWaitTime, maxWaitTime);
                }
            }
            return;
        }

        UpdateTarget();
        PlayerStats targetStats = player != null ? player.GetComponentInParent<PlayerStats>() : null;

        if (player != null && targetStats != null)
        {
            isWaitingAtPatrolPoint = false;
            if (agent != null) agent.speed = enemy.chaseSpeed;

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
        else
        {
            if (enemy.parentArea == null) return;
            if (agent != null) agent.speed = patrolSpeed;

            if (isWaitingAtPatrolPoint)
            {
                patrolWaitTimer -= Time.deltaTime;
                if (patrolWaitTimer <= 0f)
                {
                    patrolTarget = enemy.parentArea.GetValidPatrolPoint();
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(patrolTarget);
                    }
                    isWaitingAtPatrolPoint = false;
                    
                    enemy.netIsWalking.Value = true;
                    enemy.netIsChasing.Value = false;
                }
            }
            else
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    Vector2 moveDir = agent.velocity.normalized;
                    if (moveDir.sqrMagnitude > 0.01f) enemy.netDirection.Value = moveDir;

                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    {
                        StopMovement();
                        isWaitingAtPatrolPoint = true;
                        patrolWaitTimer = Random.Range(minWaitTime, maxWaitTime);
                    }
                }
            }
        }
    }

    private void OnDisable()
    {
        SetBattleState(false);
        hasCalledForHelp = false;
    }

    private void ChasePlayer()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            Vector2 direction = (agent.steeringTarget - transform.position).normalized;
            if (direction != Vector2.zero)
            {
                enemy.netDirection.Value = direction;
            }
        }
        
        enemy.netIsChasing.Value = true;
        enemy.netIsWalking.Value = false;
    }

    public void StopMovement()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (enemy.rb != null) enemy.rb.linearVelocity = Vector2.zero;
        
        enemy.netIsWalking.Value = false;
        enemy.netIsChasing.Value = false;
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

        enemy.hasDealtDamageThisAttack = true; 
        StartCoroutine(AutoUnlockDamageRoutine());

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null) playerCollider = player.GetComponentInParent<Collider2D>();
            if (playerCollider == null) playerCollider = player.GetComponentInChildren<Collider2D>();

            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();

            Vector2 targetCenter = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.position;
            Vector2 attackerCenter = enemyCollider != null ? (Vector2)enemyCollider.bounds.center : (Vector2)transform.position;

            Vector2 targetHitPoint = playerCollider != null ? playerCollider.ClosestPoint(attackerCenter) : targetCenter;

            if (Vector2.Distance(attackerCenter, targetHitPoint) <= enemy.attackRange)
            {
                if (enemy.attackType == EnemyAttackType.Directional)
                {
                    Vector2 dirToTargetCenter = (targetCenter - attackerCenter).normalized;
                    Vector2 currentFacingDir = enemy.netDirection.Value; 
                    
                    float angle = Vector2.Angle(currentFacingDir, dirToTargetCenter);

                    if (angle > enemy.attackAngle / 2f)
                    {
                        return;
                    }
                }

                var health = player.GetComponentInParent<PlayerVitals>();
                if (health != null && !health.isInvincible && !health.isProcessingDeath)
                {
                    health.TakeDamage((int)enemy.damage);
                }
            }
        }
    }

    private IEnumerator AutoUnlockDamageRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        if (enemy != null)
        {
            enemy.hasDealtDamageThisAttack = false;
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

    private void OnDrawGizmosSelected()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
        if (enemy == null || enemy.data == null) return;

        Collider2D col = GetComponent<Collider2D>();
        Vector3 pos = col != null ? col.bounds.center : transform.position;
        float range = enemy.data.attackRange;

        if (enemy.data.attackType == EnemyAttackType.AOE)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, range);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Vector2 dir = Application.isPlaying && enemy.netDirection.Value != Vector2.zero ? enemy.netDirection.Value : Vector2.down;

            Vector3 rightLimit = Quaternion.Euler(0, 0, enemy.data.attackAngle / 2f) * dir;
            Vector3 leftLimit = Quaternion.Euler(0, 0, -enemy.data.attackAngle / 2f) * dir;

            Gizmos.DrawLine(pos, pos + rightLimit * range);
            Gizmos.DrawLine(pos, pos + leftLimit * range);
            
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(pos, range);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

[System.Serializable]
public class BossPhaseInfo
{
    [TextArea] public string phaseDescription = "Phase Description";
    public int maxHealth = 1000;
}

public class Enemy : NetworkBehaviour
{
    [Header("Quest Settings")]
    public string questTargetID;
    public bool isQuestEnemy = false;
    public string UniqueID;

    [Header("Enemy Info")]
    public EnemyRank enemyRank = EnemyRank.Normal;

    [Header("Boss Phases System")]
    public List<BossPhaseInfo> bossPhases = new List<BossPhaseInfo>();
    protected int currentPhaseIndex = 0;

    public string enemyName = "Enemy";
    public int levelEnemy = 1;
    public float damage = 10f;
    public int maxHealth = 100;
    public int defense = 0;
    public float experienceReward;
    public float goldReward;

    public NetworkVariable<int> netHealth = new NetworkVariable<int>(100);
    public NetworkVariable<bool> netIsWalking = new NetworkVariable<bool>(false);
    public NetworkVariable<Vector2> netDirection = new NetworkVariable<Vector2>(Vector2.zero);

    [Header("Movement")]
    public float chaseSpeed = 3f;
    public float detectionRadius = 15f;
    public float attackRange = 2f;
    public float attackTriggerBuffer = 1f;
    public float chaseResumeBuffer = -2f;

    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;
    protected float lastAttackTime = -999f;

    [Header("Hurt & Knockback Settings")]
    public float hurtDuration = 0.5f;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;

    protected bool isKnockedBack = false;
    protected Transform player;
    private List<Transform> playersInRange = new List<Transform>();
    protected Rigidbody2D rb;
    protected EnemyAnimator enemyAnimator;

    protected bool isAttacking = false;
    protected bool isStunned = false;
    protected bool isDead = false;
    public bool IsDead => isDead;
    protected bool hasDealtDamageThisAttack = false;
    protected bool isTransitioning = false;
    protected Coroutine hurtCoroutine;
    protected bool hasProcessedDeath = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        if (string.IsNullOrEmpty(questTargetID)) questTargetID = enemyName;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (enemyAnimator != null) enemyAnimator.enabled = true;

        if (IsServer)
        {
            InitializePhase(0);
        }

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
        {
            BossHUD.Instance.ShowBossHealth(this);
        }

        if (isQuestEnemy)
        {
            if (SaveController.IsDataLoaded) CheckPersistence();
            else SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    protected void InitializePhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;
        if (bossPhases != null && bossPhases.Count > phaseIndex)
        {
            maxHealth = bossPhases[phaseIndex].maxHealth;
        }
        netHealth.Value = maxHealth;

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null && IsClient)
        {
            BossHUD.Instance.UpdatePhaseInfo(this);
        }
    }

    public string GetCurrentPhaseName() => enemyName;

    public string GetCurrentPhaseDescription()
    {
        if (bossPhases != null && bossPhases.Count > currentPhaseIndex) return bossPhases[currentPhaseIndex].phaseDescription;
        return "";
    }

    public int GetRemainingPhases()
    {
        if (bossPhases == null) return 0;
        return (bossPhases.Count - 1) - currentPhaseIndex;
    }

    public void OnPlayerDetected(Transform detectedPlayer)
    {
        if (!IsServer) return;
        if (!playersInRange.Contains(detectedPlayer)) playersInRange.Add(detectedPlayer);
    }

    public void OnPlayerLost(Transform lostPlayer)
    {
        if (!IsServer) return;
        if (playersInRange.Contains(lostPlayer)) playersInRange.Remove(lostPlayer);

        if (playersInRange.Count == 0)
        {
            player = null;
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

    protected virtual void Update()
    {
        if (!IsServer) return;

        UpdateTarget();

        if (isDead || isTransitioning || netHealth.Value <= 0 || player == null)
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            PlayerStats.IsOnBattle = true;
        }

        if (distanceToPlayer <= attackRange - attackTriggerBuffer)
        {
            StopMovement();
            netDirection.Value = (player.position - transform.position).normalized;
            if (Time.time >= lastAttackTime + attackCooldown) PerformAttack();
        }
        else
        {
            ChasePlayer();
        }
    }

    protected void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
        netDirection.Value = direction;
        netIsWalking.Value = true;
    }

    protected void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        netIsWalking.Value = false;
    }

    protected virtual void PerformAttack()
    {
        isAttacking = true;
        hasDealtDamageThisAttack = false;
        StopMovement();

        PerformAttackClientRpc(netDirection.Value);
    }

    [ClientRpc]
    private void PerformAttackClientRpc(Vector2 attackDirection)
    {
        if (isDead) return;

        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.SetFacingDirection(attackDirection);
            enemyAnimator.TriggerAttack();
        }
    }

    public virtual void DealDamage()
    {
        if (!IsServer || isDead || isStunned || hasDealtDamageThisAttack || PauseController.IsGamePause) return;
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            var health = player.GetComponentInParent<PlayerStats>();
            if (health != null && !health.isInvincible && !health.IsProcessingDeath)
            {
                health.TakeDamage((int)damage);
                hasDealtDamageThisAttack = true;
            }
        }
    }

    public void EndAttack()
    {
        if (!IsServer) return;
        isAttacking = false;
        lastAttackTime = Time.time;

        if (netHealth.Value <= 0 && !isDead)
        {
            isDead = true;
            Die();
        }
    }

    public void TakeDamage(int rawDamage, DamageSourceType damageSourceType, Transform attacker = null, bool isCritical = false, bool forceKnockback = false)
    {
        if (!IsServer || isDead || isTransitioning) return;

        float reductionMultiplier = 100f / (defense + 100f);
        int finalDamage = Mathf.Max(Mathf.CeilToInt(rawDamage * reductionMultiplier), 1);

        netHealth.Value -= finalDamage;

        TakeDamageVisualsClientRpc(finalDamage, damageSourceType, isCritical);

        if (netHealth.Value <= 0 && !isDead)
        {
            isAttacking = false;
            isStunned = false;
            isKnockedBack = false;
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);

            if (bossPhases != null && currentPhaseIndex < bossPhases.Count - 1)
            {
                StartCoroutine(SwitchPhaseRoutine());
            }
            else
            {
                isDead = true;
                Die();
            }
            return;
        }

        if (netHealth.Value > 0)
        {
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);

            isStunned = false;
            bool shouldStun = isCritical || forceKnockback;

            if (!shouldStun && attacker != null)
            {
                var pStats = attacker.GetComponentInParent<PlayerStats>();
                if (pStats != null && pStats.level > levelEnemy + 5 && isCritical)
                {
                    shouldStun = true;
                }
            }

            if (attacker != null && enemyRank != EnemyRank.Boss && shouldStun)
            {
                ApplyKnockback(attacker);
            }

            if (shouldStun)
            {
                if (isAttacking) isAttacking = false;
                hurtCoroutine = StartCoroutine(HurtRoutine());
            }
        }
    }

    [ClientRpc]
    private void TakeDamageVisualsClientRpc(int finalDamage, DamageSourceType damageSourceType, bool isCritical)
    {
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.DamagePopupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(LoadResourceManager.Instance.DamagePopupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(finalDamage, damageSourceType, isCritical);
        }

        if (isCritical && CinemachineShaker.Instance != null)
        {
            CinemachineShaker.Instance.TriggerShake(2f, 2f, 0.2f);
        }
    }

    public void ApplyKnockback(Transform attackerTransform)
    {
        if (isDead || isTransitioning) return;
        StartCoroutine(KnockbackRoutine(attackerTransform));
    }

    protected IEnumerator KnockbackRoutine(Transform attackerTransform)
    {
        isKnockedBack = true;
        isAttacking = false;
        Vector2 direction = (transform.position - attackerTransform.position).normalized;
        rb.linearVelocity = direction * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    protected IEnumerator SwitchPhaseRoutine()
    {
        isTransitioning = true;
        isStunned = true;
        StopMovement();

        SwitchPhaseVisualsClientRpc();

        yield return new WaitForSeconds(2.0f);

        int nextPhase = currentPhaseIndex + 1;
        InitializePhase(nextPhase);
        OnPhaseChange(nextPhase);

        isTransitioning = false;
        isStunned = false;
    }

    [ClientRpc]
    private void SwitchPhaseVisualsClientRpc()
    {
        if (isDead) return;

        if (enemyAnimator != null) enemyAnimator.TriggerDie();
    }

    protected virtual void OnPhaseChange(int nextPhaseIndex)
    {
        if (enemyAnimator != null)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.Play("Idle");
            enemyAnimator.SetWalking(false);
        }
    }

    protected IEnumerator HurtRoutine()
    {
        isStunned = true;
        isAttacking = false;
        if (!isKnockedBack) StopMovement();

        TriggerHurtClientRpc();

        yield return new WaitForSeconds(hurtDuration);
        isStunned = false;
        hurtCoroutine = null;
    }

    [ClientRpc]
    private void TriggerHurtClientRpc()
    {
        if (isDead) return;

        if (enemyAnimator != null) enemyAnimator.TriggerHurt();
    }

    protected virtual void Die()
    {
        if (hasProcessedDeath) return;
        hasProcessedDeath = true;

        isStunned = false;
        isAttacking = false;
        isKnockedBack = false;

        PlayerStats.IsOnBattle = false;

        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        StopMovement();

        int expGain = Mathf.FloorToInt(experienceReward * Random.Range(0.9f, 1.1f));
        int goldGain = Mathf.FloorToInt(goldReward * Random.Range(0.9f, 1.1f));

        if (PlayerStats.Instance != null && expGain > 0)
            PlayerStats.Instance.AddEXP(expGain);

        if (EconomyService.Instance != null && goldGain > 0)
        {
            EconomyService.Instance.EarnCurrency("Coin", goldGain, $"Kill: {enemyName}", (success) => {
                if (success && PlayerStats.Instance != null)
                    PlayerStats.Instance.SyncCoinFromServer(PlayerStats.Instance.coin + goldGain);
            });
        }

        if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID))
            QuestController.Instance.MarkEnemyDefeated(questTargetID);

        if (isQuestEnemy && SaveController.Instance != null && !string.IsNullOrEmpty(UniqueID))
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, UniqueID);
            SaveController.Instance.TriggerAutoSave();
        }

        DieVisualsClientRpc();
    }

    [ClientRpc]
    private void DieVisualsClientRpc()
    {
        isDead = true;

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
            BossHUD.Instance.HideBossHealth();

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Hurt");
            anim.SetBool("IsAttacking", false);

            anim.SetTrigger("isDie");
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.TriggerDie();
        }
    }

    protected virtual void Dead()
    {
        StopAllCoroutines();

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
        }

        gameObject.SetActive(false);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (isQuestEnemy) SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        CheckPersistence();
    }

    private void CheckPersistence()
    {
        if (SaveController.Instance != null && !string.IsNullOrEmpty(UniqueID))
        {
            if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, UniqueID))
            {
                if (NetworkObject != null && NetworkObject.IsSpawned && IsServer)
                {
                    NetworkObject.Despawn(false);
                }
                gameObject.SetActive(false);
            }
        }
    }
}
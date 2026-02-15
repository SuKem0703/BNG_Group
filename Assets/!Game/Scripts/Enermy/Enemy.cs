using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

public class Enemy : MonoBehaviour
{
    [Header("Quest Settings")]
    public string questTargetID;
    public bool isQuestEnemy = false;
    public string uniqueSaveID;

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
    public int currentHealth;
    public float experienceReward;
    public float goldReward;

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

    // Các biến cho Knockback
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    protected bool isKnockedBack = false;

    protected PlayerStats playerStats;
    protected Transform player;
    protected Rigidbody2D rb;
    protected EnemyAnimator enemyAnimator;

    protected bool isAttacking = false;
    protected bool isStunned = false;
    protected bool isDead = false;
    protected bool hasDealtDamageThisAttack = false;
    protected bool isTransitioning = false;
    protected Coroutine hurtCoroutine;

    protected bool hasProcessedDeath = false;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(uniqueSaveID)) uniqueSaveID = GlobalHelper.GenerateUniqueID(gameObject);
        if (string.IsNullOrEmpty(questTargetID)) questTargetID = enemyName;
    }

    protected virtual void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        player = GameObject.FindGameObjectWithTag("PlayerController")?.transform;
        rb = GetComponent<Rigidbody2D>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        GameObject detectionArea = new GameObject("DetectionArea");
        detectionArea.transform.SetParent(transform);
        detectionArea.transform.localPosition = Vector3.zero;
        CircleCollider2D detectionCollider = detectionArea.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;
        detectionArea.AddComponent<EnemyDetection>().enemyChase = this;

        InitializePhase(0);

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
        currentHealth = maxHealth;

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
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
    public bool IsDefeated() => isDead;

    protected virtual void Update()
    {
        // Check chết
        if (!isDead && !isTransitioning && currentHealth <= 0)
        {
            isAttacking = false;
            isStunned = false;
            isKnockedBack = false; // Reset knockback nếu chết

            if (bossPhases != null && currentPhaseIndex < bossPhases.Count - 1)
            {
                if (!isTransitioning) StartCoroutine(SwitchPhaseRoutine());
            }
            else
            {
                isDead = true;
                Die();
            }
            return;
        }

        if (player == null) return;

        // Failsafe Attack
        if (isAttacking && Time.time - lastAttackTime > 3.0f) EndAttack();

        // [QUAN TRỌNG] Logic chặn di chuyển
        // Thêm isKnockedBack vào điều kiện chặn
        if (PauseController.IsGamePause || isStunned || isDead || isAttacking || isTransitioning || isKnockedBack)
        {
            // [FIX LOGIC] Nếu đang bị đẩy lùi (KnockedBack) thì KHÔNG gọi StopMovement() 
            // Vì StopMovement() set vận tốc = 0, làm mất lực đẩy ngay lập tức.
            if (!isKnockedBack) StopMovement();

            if (!isAttacking && !isTransitioning && enemyAnimator != null) enemyAnimator.SetWalking(false);
            return;
        }

        // Logic AI di chuyển bình thường
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float actualTriggerDistance = attackRange - attackTriggerBuffer;

        if (distanceToPlayer <= detectionRadius)
        {
            PlayerStats.IsOnBattle = true;

            if (distanceToPlayer <= actualTriggerDistance)
            {
                StopMovement();
                if (distanceToPlayer > 0.1f && enemyAnimator != null)
                {
                    Vector2 directionToPlayer = player.position - transform.position;
                    enemyAnimator.SetFacingDirection(directionToPlayer);
                }
                if (enemyAnimator != null) enemyAnimator.SetWalking(false);

                if (Time.time >= lastAttackTime + attackCooldown) PerformAttack();
            }
            else if (distanceToPlayer > actualTriggerDistance + chaseResumeBuffer)
            {
                ChasePlayer();
            }
            else
            {
                StopMovement();
                if (enemyAnimator != null) enemyAnimator.SetWalking(false);
                if (distanceToPlayer > 0.1f && enemyAnimator != null)
                {
                    Vector2 directionToPlayer = player.position - transform.position;
                    enemyAnimator.SetFacingDirection(directionToPlayer);
                }
            }
        }
        else { StopMovement(); if (enemyAnimator != null) enemyAnimator.SetWalking(false); }
    }

    protected void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
        if (enemyAnimator != null) enemyAnimator.SetWalking(true);
    }

    protected void StopMovement() { rb.linearVelocity = Vector2.zero; }

    protected virtual void PerformAttack()
    {
        isAttacking = true;
        hasDealtDamageThisAttack = false;
        StopMovement();
        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.TriggerAttack();
        }
    }

    public virtual void DealDamage()
    {
        if (isDead || isStunned || hasDealtDamageThisAttack || PauseController.IsGamePause) return;
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            var health = player.GetComponentInParent<PlayerStats>();
            if (health != null)
            {
                // Respect invincibility and death-processing flags on player
                if (!health.isInvincible && !health.IsProcessingDeath)
                {
                    health.TakeDamage((int)damage);
                    hasDealtDamageThisAttack = true;
                }
            }
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Die();
        }
    }

    public void TakeDamage(int rawDamage, DamageSourceType damageSourceType, Transform attacker = null, bool isCritical = false)
    {
        if (isDead || isTransitioning) return;

        float reductionMultiplier = 100f / (defense + 100f);
        int finalDamage = Mathf.CeilToInt(rawDamage * reductionMultiplier);
        finalDamage = Mathf.Max(finalDamage, 1);

        currentHealth -= finalDamage;

        // --- POPUP ---
        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(finalDamage, damageSourceType, isCritical);
        }

        // --- XỬ LÝ CHẾT ---
        if (currentHealth <= 0 && !isDead)
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

        // --- XỬ LÝ KHI CÒN SỐNG (HURT / KNOCKBACK / SHAKE) ---
        if (currentHealth > 0)
        {
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);

            isStunned = false;
            bool shouldStun = false;

            // 1. KIỂM TRA ĐIỀU KIỆN STUN (Chênh lệch Level + Crit)
            bool isLevelHighEnough = playerStats != null && playerStats.level > levelEnemy + 5;

            if (isLevelHighEnough && isCritical)
            {
                shouldStun = true;
            }

            // 2. LOGIC ĐẨY LÙI (Phải thỏa mãn shouldStun mới được đẩy)
            if (attacker != null && !isDead && enemyRank != EnemyRank.Boss && shouldStun)
            {
                ApplyKnockback(attacker);
            }

            // 3. Rung Camera (Vẫn giữ nguyên khi Crit)
            if (isCritical && CinemachineShaker.Instance != null)
            {
                CinemachineShaker.Instance.TriggerShake(2f, 2f, 0.2f);
            }

            // 4. Kích hoạt Animation Stun
            if (shouldStun)
            {
                if (isAttacking) isAttacking = false;
                hurtCoroutine = StartCoroutine(HurtRoutine());
            }
        }
    }

    // --- COROUTINE KNOCKBACK MỚI ---
    public void ApplyKnockback(Transform attackerTransform)
    {
        if (isDead || isTransitioning) return;
        StartCoroutine(KnockbackRoutine(attackerTransform));
    }

    protected IEnumerator KnockbackRoutine(Transform attackerTransform)
    {
        isKnockedBack = true;
        isAttacking = false;

        // Tính hướng từ người đánh -> quái
        Vector2 direction = (transform.position - attackerTransform.position).normalized;

        // Đẩy đi
        rb.linearVelocity = direction * knockbackForce;

        yield return new WaitForSeconds(knockbackDuration);

        // Dừng lại
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    protected IEnumerator SwitchPhaseRoutine()
    {
        isTransitioning = true;
        isStunned = true;
        StopMovement();

        if (enemyAnimator != null) enemyAnimator.TriggerDie();

        yield return new WaitForSeconds(2.0f);

        int nextPhase = currentPhaseIndex + 1;
        InitializePhase(nextPhase);
        OnPhaseChange(nextPhase);

        if (BossHUD.Instance != null) BossHUD.Instance.UpdatePhaseInfo(this);

        isTransitioning = false;
        isStunned = false;
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

        if (enemyAnimator != null) enemyAnimator.TriggerHurt();

        // Nếu không bị knockback thì mới StopMovement ở đây, còn đang knockback thì để lực đẩy lo
        if (!isKnockedBack) StopMovement();

        yield return new WaitForSeconds(hurtDuration);
        isStunned = false;
        hurtCoroutine = null;
    }

    protected virtual void Die()
    {
        if (hasProcessedDeath) return;
        hasProcessedDeath = true;

        isStunned = false;
        isAttacking = false;
        isKnockedBack = false;

        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        StopMovement();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.Play("Dead");
            else enemyAnimator.TriggerDie();
        }

        PlayerStats.IsOnBattle = false;

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
        {
            BossHUD.Instance.HideBossHealth();
        }

        int expGain = Mathf.FloorToInt(experienceReward * Random.Range(0.9f, 1.1f));
        int goldGain = Mathf.FloorToInt(goldReward * Random.Range(0.9f, 1.1f));

        if (PlayerStats.Instance != null && expGain > 0)
        {
            PlayerStats.Instance.AddEXP(expGain);
        }

        if (EconomyService.Instance != null && goldGain > 0)
        {
            EconomyService.Instance.EarnCurrency("Coin", goldGain, $"Kill: {enemyName}", (success) =>
            {
                if (success)
                {
                    if (PlayerStats.Instance != null)
                        PlayerStats.Instance.SyncCoinFromServer(PlayerStats.Instance.coin + goldGain);
                }
            });
        }
        else
        {
            
        }

        if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID))
            QuestController.Instance.MarkEnemyDefeated(questTargetID);

        if (isQuestEnemy && SaveController.Instance != null && !string.IsNullOrEmpty(uniqueSaveID))
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, uniqueSaveID);
            SaveController.Instance.TriggerAutoSave();
        }
    }

    protected virtual void Dead() { StopAllCoroutines(); Destroy(gameObject); }
    public void OnPlayerDetected(Transform detectedPlayer) { player = detectedPlayer; }
    public void OnPlayerLost() { player = null; StopMovement(); if (enemyAnimator != null) enemyAnimator.SetWalking(false); }

    private void OnDestroy() { if (isQuestEnemy) SaveController.OnDataLoaded -= HandleDataLoaded; }
    private void HandleDataLoaded() { SaveController.OnDataLoaded -= HandleDataLoaded; CheckPersistence(); }
    private void CheckPersistence() { if (SaveController.Instance != null && !string.IsNullOrEmpty(uniqueSaveID)) { if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, uniqueSaveID)) Destroy(gameObject); } }
}
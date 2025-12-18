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
    public float attackTriggerBuffer = 1f; // Khoảng cách trừ hao để bắt đầu dừng lại
    public float chaseResumeBuffer = -2f;  // Số âm để luôn đuổi nếu trượt ra khỏi tầm

    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;
    protected float lastAttackTime = -999f;

    [Header("Hurt Settings")]
    public float hurtDuration = 1f;

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
    }

    public string GetCurrentPhaseName()
    {
        return enemyName;
    }

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
        if (!isDead && !isTransitioning && currentHealth <= 0)
        {
            if (bossPhases != null && currentPhaseIndex < bossPhases.Count - 1)
            {
                StartCoroutine(SwitchPhaseRoutine());
            }
            else
            {
                isDead = true;
                Die();
                Dead();
            }
            return;
        }

        if (player == null) return;

        // Failsafe: Reset nếu kẹt Attack quá lâu
        if (isAttacking && Time.time - lastAttackTime > 3.0f)
        {
            EndAttack();
        }

        if (PauseController.IsGamePause || isStunned || isDead || isAttacking || isTransitioning)
        {
            StopMovement();
            if (!isAttacking && !isTransitioning && enemyAnimator != null) enemyAnimator.SetWalking(false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float actualTriggerDistance = attackRange - attackTriggerBuffer; // Ví dụ: 2 - 1 = 1m

        if (distanceToPlayer <= detectionRadius)
        {
            PlayerStats.IsOnBattle = true;

            // LOGIC DI CHUYỂN TÍCH HỢP BUFFER ÂM
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
                health.TakeDamage((int)damage);
                hasDealtDamageThisAttack = true;
            }
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
    }

    public void TakeDamage(int rawDamage, DamageSourceType damageSourceType)
    {
        if (isDead || isTransitioning) return;

        float reductionMultiplier = 100f / (defense + 100f);
        int finalDamage = Mathf.CeilToInt(rawDamage * reductionMultiplier);

        finalDamage = Mathf.Max(finalDamage, 1);

        currentHealth -= finalDamage;

        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(finalDamage, damageSourceType);
        }

        if (currentHealth <= 0)
        {
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

        if (currentHealth > 0)
        {
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
            isStunned = false; // Reset stun cũ

            bool shouldStun = false;
            if ((playerStats != null && playerStats.level > levelEnemy + 5) || enemyRank != EnemyRank.Boss) shouldStun = true;
            if (shouldStun)
            {
                if (isAttacking) isAttacking = false;
                hurtCoroutine = StartCoroutine(HurtRoutine());
            }
        }
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
        // Reset animator để Boss đứng dậy
        if (enemyAnimator != null)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.Play("Idle");
            enemyAnimator.SetWalking(false);
        }
    }

    protected IEnumerator HurtRoutine() { isStunned = true; if (enemyAnimator != null) enemyAnimator.TriggerHurt(); StopMovement(); yield return new WaitForSeconds(hurtDuration); isStunned = false; hurtCoroutine = null; }

    protected virtual void Die()
    {
        isStunned = true;
        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        StopMovement();
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.TriggerDie();
        }
        PlayerStats.IsOnBattle = false;
        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
        {
            BossHUD.Instance.HideBossHealth();
        }
        int expGain = Mathf.FloorToInt(experienceReward * Random.Range(0.9f, 1.1f));
        PlayerStats.Instance.AddEXP(expGain);
        int goldGain = Mathf.FloorToInt(goldReward * Random.Range(0.9f, 1.1f));
        PlayerStats.Instance.AddCoin(goldGain);
        if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID)) QuestController.Instance.MarkEnemyDefeated(questTargetID);
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
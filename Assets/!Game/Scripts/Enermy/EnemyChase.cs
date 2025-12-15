using System.Collections;
using Unity.Cinemachine.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyChase : MonoBehaviour
{
    [Header("Quest Settings")]
    public string questTargetID;
    public bool isQuestEnemy = false;
    public string uniqueSaveID;

    [Header("Enemy Info")]
    public string enemyName = "Slime";
    public int levelEnemy = 1;
    public float damage = 10f;
    public int maxHealth = 100;
    public int currentHealth;
    private float experienceReward;
    private float goldReward;

    [Header("Movement")]
    public float chaseSpeed = 3f;
    public float detectionRadius = 5f;
    public float attackRange = 1f;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;
    private float lastAttackTime = -999f;

    [Header("Hurt Settings")]
    public float hurtDuration = 1f;

    private PlayerStats playerStats;
    private Transform player;
    private Rigidbody2D rb;
    private EnemyAnimator enemyAnimator;

    // State flags
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDead = false;
    private bool hasDealtDamageThisAttack = false;
    private Coroutine hurtCoroutine;

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueSaveID)) uniqueSaveID = GlobalHelper.GenerateUniqueID(gameObject);
        if (string.IsNullOrEmpty(questTargetID)) questTargetID = enemyName;
    }

    void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        player = GameObject.FindGameObjectWithTag("PlayerController")?.transform;
        rb = GetComponent<Rigidbody2D>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        // Setup Detection Area
        GameObject detectionArea = new GameObject("DetectionArea");
        detectionArea.transform.SetParent(transform);
        detectionArea.transform.localPosition = Vector3.zero;
        CircleCollider2D detectionCollider = detectionArea.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;
        detectionArea.AddComponent<EnemyDetection>().enemyChase = this;

        currentHealth = maxHealth;

        if (isQuestEnemy)
        {
            if (SaveController.IsDataLoaded) CheckPersistence();
            else SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void OnDestroy()
    {
        if (isQuestEnemy) SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        CheckPersistence();
    }

    private void CheckPersistence()
    {
        if (SaveController.Instance != null && !string.IsNullOrEmpty(uniqueSaveID))
        {
            if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, uniqueSaveID))
                Destroy(gameObject);
        }
    }

    void Update()
    {
        if (player == null) return;

        if (PauseController.IsGamePause || isStunned || isDead || isAttacking)
        {
            StopMovement();

            if (!isAttacking && enemyAnimator != null) enemyAnimator.SetWalking(false);

            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            PlayerStats.IsOnBattle = true;

            if (distanceToPlayer <= attackRange)
            {
                StopMovement();

                if (enemyAnimator != null && !isAttacking)
                {
                    if (distanceToPlayer > 0.1f)
                    {
                        Vector2 directionToPlayer = player.position - transform.position;
                        enemyAnimator.SetFacingDirection(directionToPlayer);
                    }
                }

                if (enemyAnimator != null) enemyAnimator.SetWalking(false);

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                }
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            StopMovement();
            if (enemyAnimator != null) enemyAnimator.SetWalking(false);
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
        if (enemyAnimator != null) enemyAnimator.SetWalking(true);
    }

    // Hàm dừng di chuyển dùng chung
    void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // Hàm kích hoạt tấn công
    void PerformAttack()
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

    public void DealDamage()
    {
        if (isDead || isStunned || hasDealtDamageThisAttack || PauseController.IsGamePause) return;

        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f)
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


    public void TakeDamage(int damage, DamageSourceType damageSourceType)
    {
        if (isDead) return;

        currentHealth -= damage;

        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(damage, damageSourceType);
        }

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Die();
            return;
        }

        if (currentHealth > 0)
        {
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);

            bool shouldStun = false;
            if (playerStats != null && playerStats.level > levelEnemy + 5)
                shouldStun = true;

            if (shouldStun)
            {
                // Nếu bị choáng khi đang đánh -> Hủy đánh ngay
                if (isAttacking)
                {
                    isAttacking = false;
                }
                hurtCoroutine = StartCoroutine(HurtRoutine());
            }
        }
    }

    private IEnumerator HurtRoutine()
    {
        isStunned = true;
        if (enemyAnimator != null) enemyAnimator.TriggerHurt();
        StopMovement();

        yield return new WaitForSeconds(hurtDuration);

        isStunned = false;
        hurtCoroutine = null;
    }

    void Die()
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

        int expGain = Mathf.FloorToInt(experienceReward * Random.Range(0.9f, 1.1f));
        PlayerStats.Instance.AddEXP(expGain);

        int goldGain = Mathf.FloorToInt(goldReward * Random.Range(0.9f, 1.1f));
        PlayerStats.Instance.AddCoin(goldGain);

        if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID))
            QuestController.Instance.MarkEnemyDefeated(questTargetID);

        if (isQuestEnemy && SaveController.Instance != null && !string.IsNullOrEmpty(uniqueSaveID))
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, uniqueSaveID);
            SaveController.Instance.TriggerAutoSave();
        }
    }

    public void Dead()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    public void OnPlayerDetected(Transform detectedPlayer)
    {
        player = detectedPlayer;
    }

    public void OnPlayerLost()
    {
        player = null;
        StopMovement();
        if (enemyAnimator != null) enemyAnimator.SetWalking(false);
    }

    // Thêm vào cuối class EnemyChase
    private void OnDrawGizmosSelected()
    {
        // Vẽ vùng phát hiện (màu vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Vẽ vùng tấn công (màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
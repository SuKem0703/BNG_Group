using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyChase : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("ID dùng để tính Quest (VD: 'slime_blue'). 3 con Slime cùng loại phải có ID này GIỐNG NHAU.")]
    public string questTargetID;

    [Tooltip("Nếu True: Enemy này sẽ lưu trạng thái chết vĩnh viễn (dùng cho Boss hoặc Quest Mobs).")]
    public bool isQuestEnemy = false;

    [Tooltip("ID dùng để Save. ĐỂ TRỐNG để tự sinh theo tọa độ (Khuyên dùng cho quái thường).")]
    public string uniqueSaveID;

    [Header("Enemy Info")]
    public string enemyName = "Slime";
    public int levelEnemy = 1;
    public float damage = 10f;
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Movement")]
    public float chaseSpeed = 3f;
    public float detectionRadius = 5f;
    public float attackRange = 1f;

    [Header("Attack Setings")]
    public float attackDelay = 1f;

    [Header("Hurt Settings")]
    [Tooltip("Thời gian choáng sau khi bị đánh")]
    public float hurtDuration = 1f;

    private PlayerStats playerStats;
    private Transform player;
    private Vector3 spawnPoint;
    private Rigidbody2D rb;

    [Header("Animation")]
    private EnemyAnimator enemyAnimator;

    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    private Coroutine hurtCoroutine;
    private bool isStunned = false;
    private bool isDead = false;
    private bool hasDealtDamageThisAttack = false;

    // Sinh ID tự động ngay khi khởi tạo
    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueSaveID))
        {
            uniqueSaveID = GlobalHelper.GenerateUniqueID(gameObject);
        }

        if (string.IsNullOrEmpty(questTargetID))
        {
            questTargetID = enemyName;
        }
    }

    // Khởi tạo và kiểm tra Save Data
    void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        player = GameObject.FindGameObjectWithTag("PlayerController")?.transform;
        spawnPoint = transform.position;

        rb = GetComponent<Rigidbody2D>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        GameObject detectionArea = new GameObject("DetectionArea");
        detectionArea.transform.SetParent(transform);
        detectionArea.transform.localPosition = Vector3.zero;
        CircleCollider2D detectionCollider = detectionArea.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;
        detectionArea.AddComponent<EnemyDetection>().enemyChase = this;

        currentHealth = maxHealth;

        // Logic check Save Data cho Quest Enemy
        if (isQuestEnemy)
        {
            if (SaveController.IsDataLoaded)
            {
                CheckPersistence();
            }
            else
            {
                SaveController.OnDataLoaded += HandleDataLoaded;
            }
        }
    }

    // Hủy đăng ký sự kiện để tránh lỗi bộ nhớ
    private void OnDestroy()
    {
        if (isQuestEnemy)
        {
            SaveController.OnDataLoaded -= HandleDataLoaded;
        }
    }

    // Xử lý khi dữ liệu load xong (trường hợp load scene chưa xong data)
    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        CheckPersistence();
    }

    // Kiểm tra xem Enemy này đã bị tiêu diệt chưa
    private void CheckPersistence()
    {
        if (SaveController.Instance != null && !string.IsNullOrEmpty(uniqueSaveID))
        {
            if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, uniqueSaveID))
            {
                Destroy(gameObject);
            }
        }
    }

    // Logic update mỗi khung hình
    void Update()
    {
        if (player == null) return;

        if (PauseController.IsGamePause || isStunned || isDead || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            if (enemyAnimator != null) enemyAnimator.SetWalking(false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            PlayerStats.IsOnBattle = true;

            if (distanceToPlayer <= attackRange && !isAttacking)
            {
                StartAttack();
            }
            else if (!isAttacking)
            {
                ChasePlayer();
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (enemyAnimator != null) enemyAnimator.SetWalking(false);
        }
    }

    // Di chuyển tới player
    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;

        if (enemyAnimator != null) enemyAnimator.SetWalking(true);
    }

    // Bắt đầu tấn công
    void StartAttack()
    {
        if (attackCoroutine == null)
        {
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
    }

    // Coroutine thực hiện tấn công
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        while (player != null && Vector2.Distance(transform.position, player.position) <= attackRange && !isStunned && !isDead)
        {
            hasDealtDamageThisAttack = false;

            if (enemyAnimator != null)
            {
                enemyAnimator.SetWalking(false);
                enemyAnimator.TriggerAttack();
            }

            yield return new WaitForSeconds(attackDelay);
        }

        isAttacking = false;
        attackCoroutine = null;
    }

    // Gây sát thương (được gọi từ Animation Event)
    public void DealDamage()
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

    // Nhận sát thương từ người chơi
    public void TakeDamage(int damage, DamageSourceType damageSourceType)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;

        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);

            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);

            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null)
            {
                popupScript.Setup(damage, damageSourceType);
            }
        }

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Die();
            return;
        }

        if (currentHealth > 0)
        {
            if (hurtCoroutine != null)
            {
                StopCoroutine(hurtCoroutine);
            }

            bool shouldStun = false;
            if (playerStats != null)
            {
                if (playerStats.level > levelEnemy + 5)
                {
                    shouldStun = true;
                }
            }

            if (shouldStun)
            {
                if (isAttacking && attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                    isAttacking = false;
                    attackCoroutine = null;
                }

                hurtCoroutine = StartCoroutine(HurtRoutine());
            }
        }
    }

    // Coroutine xử lý trạng thái bị thương
    private IEnumerator HurtRoutine()
    {
        isStunned = true;

        if (enemyAnimator != null) enemyAnimator.TriggerHurt();

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(hurtDuration);

        isStunned = false;
        hurtCoroutine = null;
    }

    // Xử lý khi chết
    void Die()
    {
        isStunned = true;

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.TriggerDie();
        }

        PlayerStats.IsOnBattle = false;

        if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID))
        {
            QuestController.Instance.MarkEnemyDefeated(questTargetID);
        }

        if (isQuestEnemy && SaveController.Instance != null && !string.IsNullOrEmpty(uniqueSaveID))
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, uniqueSaveID);
            SaveController.Instance.TriggerAutoSave();
        }
    }

    // Hàm gọi bởi Animation Event khi animation chết kết thúc
    public void Dead()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    // Kết thúc tấn công (Animation Event)
    public void EndAttack()
    {
        isAttacking = false;
    }

    // Phát hiện người chơi
    public void OnPlayerDetected(Transform detectedPlayer)
    {
        player = detectedPlayer;
    }

    // Mất dấu người chơi
    public void OnPlayerLost()
    {
        player = null;
        rb.linearVelocity = Vector2.zero;

        if (enemyAnimator != null)
            enemyAnimator.SetWalking(false);
    }
}
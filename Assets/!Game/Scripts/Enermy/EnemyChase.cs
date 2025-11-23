using System;
using System.Collections;
using UnityEngine;

public class EnemyChase : MonoBehaviour
{
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
    }

    void Update()
    {
        if (player == null) return;

        PlayerStats.IsOnBattle = true;

        if (PauseController.IsGamePause || isStunned || isDead || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            if (enemyAnimator != null) enemyAnimator.SetWalking(false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && !isAttacking)
        {
            StartAttack();
        }
        else if (distanceToPlayer <= detectionRadius && !isAttacking)
        {
            ChasePlayer();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (enemyAnimator != null) enemyAnimator.SetWalking(false);
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;

        if (enemyAnimator != null) enemyAnimator.SetWalking(true);
    }

    void StartAttack()
    {
        if (attackCoroutine == null)
        {
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
    }

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
    private IEnumerator HurtRoutine()
    {
        isStunned = true;

        if (enemyAnimator != null) enemyAnimator.TriggerHurt();

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(hurtDuration);

        isStunned = false;
        hurtCoroutine = null;
    }
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

        if (    enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.TriggerDie();
        }

        PlayerStats.IsOnBattle = false;
    }
    public void Dead()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    public void EndAttack()
    {
        isAttacking = false;
    }
    public void OnPlayerDetected(Transform detectedPlayer)
    {
        player = detectedPlayer;
    }

    public void OnPlayerLost()
    {
        player = null;
        rb.linearVelocity = Vector2.zero;

        if (enemyAnimator != null)
            enemyAnimator.SetWalking(false);
    }

}

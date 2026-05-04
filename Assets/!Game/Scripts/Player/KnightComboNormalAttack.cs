using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class KnightComboNormalAttack : NetworkBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Combo Settings")]
    public int combo = 1;
    public int comboNumber = 3;
    public float comboTiming = 2f;
    public float comboTempo = 0f;
    private float minComboInterval = 0.5f;

    [Header("Attack Settings")]
    public int runAttackStaminaCost = 1;
    public int normalAttackStaminaCost = 0;
    public float runAttackMultiplier = 1.2f;
    public bool causesKnockback = true;
    public float attackAngle = 180f;

    [Header("Hit Feedback")]
    public float hitShakeIntensity = 1.5f;
    public float hitShakeFrequency = 2f;
    public float hitShakeDuration = 0.15f;

    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

    public bool isAttacking => animator.GetBool("isAttacking");
    public bool isWalking => animator.GetBool("isWalking");
    public bool isRunning => animator.GetBool("isRunning");
    public bool isWalkAttacking => animator.GetBool("isWalkAttacking");
    public bool isRunAttacking => animator.GetBool("isRunAttacking");
    public Transform attackPoint;
    public float attackRange = 0.6f;
    public LayerMask enemyLayer;

    private bool attackPressed = false;
    private int currentComboCache;
    private List<Collider2D> enemiesHitThisAttack;
    private Vector2 attackDirection;

    [SerializeField] private CombatTargetSelector targetSelector;

    private void Start()
    {
        enemiesHitThisAttack = new List<Collider2D>();
        comboTempo = comboTiming;
        combo = 1;
        comboNumber = 3;
        minComboInterval = 0.5f;
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateAttackPointDirection();

        if (PauseController.IsGamePause)
        {
            animator.SetBool("isAttacking", false);
            animator.SetBool("isWalkAttacking", false);
            animator.SetBool("isRunAttacking", false);
            return;
        }

        comboTempo += Time.deltaTime;

        if (comboTiming > 0)
        {
            comboTiming -= Time.deltaTime;
            if (comboTiming <= 0) combo = 1;
        }

        if (isAttacking && !isWalkAttacking && !isRunAttacking)
        {
            animator.SetBool("isWalking", false);
        }

        if (attackPressed)
        {
            attackPressed = false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TryAttack();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (PauseController.IsGamePause || !context.performed) return;
        if (playerMovement != null && playerMovement.IsDead) return;

        attackPressed = true;
    }

    private void TryAttack()
    {
        if (isAttacking) return;
        if (comboTempo < minComboInterval) return;
        if (playerMovement != null && playerMovement.IsDead) return;
        if (!PlayerStats.Instance.CanAttack || !GameStateManager.CanProcessInput()) return;

        int cost = (playerMovement != null && playerMovement.isRunning) ? runAttackStaminaCost : normalAttackStaminaCost;

        if (playerStats != null && playerStats.currentStamina >= cost)
        {
            Combo(cost);
        }
    }

    public void Combo(int staminaCost)
    {
        if (comboTempo < 0) return;

        playerStats.UseStamina(staminaCost);
        animator.SetBool("isAttacking", true);

        bool isMoving = playerMovement != null && playerMovement.moveInput.magnitude > 0.1f;
        bool isRunningLocal = playerMovement != null && playerMovement.isRunning;

        if (isRunningLocal)
        {
            animator.SetBool("isRunAttacking", true);
            animator.SetBool("isWalkAttacking", false);
        }
        else
        {
            animator.SetBool("isRunAttacking", false);
            animator.SetBool("isWalkAttacking", isMoving);
        }

        if (!isMoving) animator.SetBool("isWalking", false);

        animator.SetFloat("LookX", attackDirection.x);
        animator.SetFloat("LookY", attackDirection.y);
        animator.SetFloat("LastInputX", attackDirection.x);
        animator.SetFloat("LastInputY", attackDirection.y);

        if (playerMovement != null)
        {
            playerMovement.netLastInput.Value = attackDirection;
        }

        enemiesHitThisAttack.Clear();
        currentComboCache = combo;

        animator.SetTrigger("Attack");

        if (!isRunningLocal)
        {
            combo++;
            if (combo > comboNumber) combo = 1;
        }
        else
        {
            combo = 1;
        }

        comboTempo = 0f;
        comboTiming = 2f;

        if (IsOwner)
        {
            PlayAttackAnimationServerRpc(isRunningLocal, isMoving, attackDirection, currentComboCache);
        }
    }

    [ServerRpc]
    private void PlayAttackAnimationServerRpc(bool runAttack, bool walkAttack, Vector2 dir, int comboCount)
    {
        PlayAttackAnimationClientRpc(runAttack, walkAttack, dir, comboCount);
    }

    [ClientRpc]
    private void PlayAttackAnimationClientRpc(bool runAttack, bool walkAttack, Vector2 dir, int comboCount)
    {
        if (IsOwner) return;

        animator.SetBool("isAttacking", true);
        animator.SetBool("isRunAttacking", runAttack);
        animator.SetBool("isWalkAttacking", walkAttack);

        if (!runAttack && !walkAttack) animator.SetBool("isWalking", false);

        animator.SetFloat("LookX", dir.x);
        animator.SetFloat("LookY", dir.y);
        animator.SetFloat("LastInputX", dir.x);
        animator.SetFloat("LastInputY", dir.y);

        animator.SetTrigger("Attack");
    }

    public void StartAttack()
    {
        SoundEffectManager.Play("Melee Effect", true);
    }

    public void EndAttack()
    {
        animator.SetBool("isAttacking", false);
        animator.SetBool("isWalkAttacking", false);
        animator.SetBool("isRunAttacking", false);
        animator.ResetTrigger("Attack");
        attackPressed = false;
    }

    public void DealDamageEvent()
    {
        if (!IsOwner) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        float stanceMultiplier = 1.0f;
        if (isRunAttacking) stanceMultiplier = runAttackMultiplier;
        else if (!isWalkAttacking) stanceMultiplier = 1.5f;

        bool hasHitAnyEnemy = false;
        Vector3 basePosition = transform.parent != null ? transform.parent.position : transform.position;

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (enemiesHitThisAttack.Contains(enemyCollider)) continue;

            Vector2 dirToEnemy = (enemyCollider.transform.position - basePosition).normalized;
            if (Vector2.Angle(attackDirection, dirToEnemy) > attackAngle / 2f)
            {
                continue;
            }

            Enemy enemyChase = enemyCollider.GetComponent<Enemy>();

            if (enemyChase != null && !enemyChase.IsDead && enemyChase.netHealth.Value > 0)
            {
                hasHitAnyEnemy = true;

                float comboScale = checkCombo(currentComboCache);
                if (isRunAttacking) comboScale = 1.0f;

                float rawDamage = playerStats.finalPhysicalAttack * comboScale * stanceMultiplier;

                bool isCritical = false;
                if (UnityEngine.Random.Range(0f, 100f) < playerStats.finalCritRate)
                {
                    isCritical = true;
                    rawDamage *= 2;
                }

                int finalDamage = Mathf.RoundToInt(rawDamage);
                bool forceKnockback = isRunAttacking && causesKnockback;

                if (IsServer)
                {
                    enemyChase.TakeDamage(finalDamage, DamageSourceType.Knight, transform.parent, isCritical, forceKnockback);
                }
                else
                {
                    NetworkObject enemyNetObj = enemyChase.GetComponent<NetworkObject>();
                    if (enemyNetObj != null)
                    {
                        RequestDealDamageServerRpc(enemyNetObj.NetworkObjectId, finalDamage, isCritical, forceKnockback);
                    }
                }

                enemiesHitThisAttack.Add(enemyCollider);
            }
        }

        if (hasHitAnyEnemy && CinemachineShaker.Instance != null)
        {
            CinemachineShaker.Instance.TriggerShake(hitShakeIntensity, hitShakeFrequency, hitShakeDuration);
        }
    }

    [ServerRpc]
    private void RequestDealDamageServerRpc(ulong enemyNetworkId, int finalDamage, bool isCritical, bool forceKnockback, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemyNetworkId, out NetworkObject enemyObj))
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                Transform attackerTransform = null;
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client))
                {
                    attackerTransform = client.PlayerObject.transform;
                }

                enemy.TakeDamage(finalDamage, DamageSourceType.Knight, attackerTransform, isCritical, forceKnockback);
            }
        }
    }

    private float checkCombo(int c)
    {
        switch (c)
        {
            case 1: return 1.0f;
            case 2: return 1.2f;
            case 3: return 1.5f;
            default: return 1.0f;
        }
    }

    private void UpdateAttackPointDirection()
    {
        if (Camera.main == null || isAttacking) return;

        Vector3 basePosition = transform.parent != null ? transform.parent.position : transform.position;

        Vector2 fallbackDir = playerMovement != null && playerMovement.moveInput.magnitude > 0.01f
            ? playerMovement.moveInput.normalized
            : attackDirection;

        if (targetSelector != null)
        {
            attackDirection = targetSelector.GetAimDirection(basePosition, fallbackDir);
        }
        else
        {
            attackDirection = fallbackDir;
        }

        attackPoint.position = basePosition + (Vector3)attackDirection * attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Vector3 basePosition = transform.parent != null ? transform.parent.position : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(basePosition, attackPoint.position);

        if (attackDirection != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Vector3 rightLimit = Quaternion.Euler(0, 0, attackAngle / 2f) * attackDirection;
            Vector3 leftLimit = Quaternion.Euler(0, 0, -attackAngle / 2f) * attackDirection;

            float reach = Vector3.Distance(basePosition, attackPoint.position) + attackRange;
            Gizmos.DrawLine(basePosition, basePosition + rightLimit * reach);
            Gizmos.DrawLine(basePosition, basePosition + leftLimit * reach);
        }
    }
}
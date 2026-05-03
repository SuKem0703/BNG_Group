using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class KnightComboNormalAttack : NetworkBehaviour
{
    private Animator ani;

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

    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

    public bool isAttacking => ani.GetBool("isAttacking");
    public bool isWalking => ani.GetBool("isWalking");
    public bool isRunning => ani.GetBool("isRunning");
    public bool isWalkAttacking => ani.GetBool("isWalkAttacking");
    public bool isRunAttacking => ani.GetBool("isRunAttacking");

    public Transform attackPoint;
    public float attackRange = 0.6f;
    public LayerMask enemyLayer;

    private bool attackPressed = false;
    private int currentComboCache;
    private List<Collider2D> enemiesHitThisAttack;
    private Vector2 attackDirection;

    private void Start()
    {
        ani = GetComponent<Animator>();
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
            ani.SetBool("isAttacking", false);
            ani.SetBool("isWalkAttacking", false);
            ani.SetBool("isRunAttacking", false);
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
            ani.SetBool("isWalking", false);
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
        ani.SetBool("isAttacking", true);

        bool isMoving = playerMovement != null && playerMovement.moveInput.magnitude > 0.1f;
        bool isRunningLocal = playerMovement != null && playerMovement.isRunning;

        if (isRunningLocal)
        {
            ani.SetBool("isRunAttacking", true);
            ani.SetBool("isWalkAttacking", false);
        }
        else
        {
            ani.SetBool("isRunAttacking", false);
            ani.SetBool("isWalkAttacking", isMoving);
        }

        if (!isMoving) ani.SetBool("isWalking", false);

        ani.SetFloat("LookX", attackDirection.x);
        ani.SetFloat("LookY", attackDirection.y);
        ani.SetFloat("LastInputX", attackDirection.x);
        ani.SetFloat("LastInputY", attackDirection.y);

        if (playerMovement != null)
        {
            playerMovement.netLastInput.Value = attackDirection;
        }

        enemiesHitThisAttack.Clear();
        currentComboCache = combo;

        ani.SetTrigger("Attack");

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

        ani.SetBool("isAttacking", true);
        ani.SetBool("isRunAttacking", runAttack);
        ani.SetBool("isWalkAttacking", walkAttack);

        if (!runAttack && !walkAttack) ani.SetBool("isWalking", false);

        ani.SetFloat("LookX", dir.x);
        ani.SetFloat("LookY", dir.y);
        ani.SetFloat("LastInputX", dir.x);
        ani.SetFloat("LastInputY", dir.y);

        ani.SetTrigger("Attack");
    }

    public void StartAttack()
    {
        SoundEffectManager.Play("Melee Effect", true);
    }

    public void EndAttack()
    {
        ani.SetBool("isAttacking", false);
        ani.SetBool("isWalkAttacking", false);
        ani.SetBool("isRunAttacking", false);
        ani.ResetTrigger("Attack");
        attackPressed = false;
    }

    public void DealDamageEvent()
    {
        if (!IsOwner) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        float stanceMultiplier = 1.0f;
        if (isRunAttacking) stanceMultiplier = runAttackMultiplier;
        else if (!isWalkAttacking) stanceMultiplier = 1.5f;

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (enemiesHitThisAttack.Contains(enemyCollider)) continue;

            Enemy enemyChase = enemyCollider.GetComponent<Enemy>();
            if (enemyChase != null)
            {
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
        if (Camera.main == null) return;

        if (playerMovement != null && playerMovement.isRunning && playerMovement.moveInput.magnitude > 0.01f)
        {
            attackDirection = playerMovement.moveInput.normalized;
        }
        else
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
            worldMousePos.z = 0;
            attackDirection = (worldMousePos - transform.position).normalized;
        }

        attackPoint.position = transform.position + (Vector3)attackDirection * attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, attackPoint.position);
    }
}
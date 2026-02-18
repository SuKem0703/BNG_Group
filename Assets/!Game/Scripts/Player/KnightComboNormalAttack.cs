using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class KnightComboNormalAttack : MonoBehaviour
{
    private Animator ani;

    [Header("Combo Settings")]
    public int combo = 1;
    public int comboNumber = 3;
    public float comboTiming = 2f;
    public float comboTempo = 0f;
    private float minComboInterval = 0.5f;

    [Header("Attack Settings")]
    [Tooltip("Stamina cost for running attack")]
    public int runAttackStaminaCost = 1;
    [Tooltip("Stamina cost for normal attack")]
    public int normalAttackStaminaCost = 0;
    [Tooltip("Damage multiplier for running attack")]
    public float runAttackMultiplier = 1.2f;
    [Tooltip("Knockback force flag for run attack")]
    public bool causesKnockback = true;

    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

    // We don't need a separate variable for attackStaminaCost anymore as it's dynamic

    public bool isAttacking => ani.GetBool("isAttacking");
    public bool isWalking => ani.GetBool("isWalking");
    public bool isRunning => ani.GetBool("isRunning");
    public bool isWalkAttacking => ani.GetBool("isWalkAttacking");
    public bool isRunAttacking => ani.GetBool("isRunAttacking");

    public Transform attackPoint;
    public float attackRange = 0.75f;
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

            if (comboTiming <= 0)
            {
                combo = 1;
            }
        }

        // Logic for stopping walking animation if attacking but NOT run/walk attacking
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
        if (PauseController.IsGamePause || !context.performed) return;

        // Prevent attack input if player is dead
        if (playerMovement != null && playerMovement.IsDead) return;

        attackPressed = true;
    }

    private void TryAttack()
    {
        if (isAttacking) return;

        if (comboTempo < minComboInterval) return;

        if (playerMovement != null && playerMovement.IsDead) return;

        if (!PlayerStats.Instance.CanAttack || !GameStateManager.CanProcessInput())
        {
            return;
        }

        // Determine stamina cost based on movement state
        int cost = (playerMovement != null && playerMovement.isRunning) ? runAttackStaminaCost : normalAttackStaminaCost;

        if (playerStats != null && playerStats.currentStamina >= cost)
        {
            Combo(cost);
        }
    }

    public void Combo(int staminaCost)
    {
        if (comboTempo < 0)
        {
            Debug.Log("Error! Tempo cannot be < 0.");
            return;
        }

        playerStats.UseStamina(staminaCost);

        ani.SetBool("isAttacking", true);

        bool isMoving = playerMovement != null && playerMovement.moveInput.magnitude > 0.1f;
        bool isRunning = playerMovement != null && playerMovement.isRunning;

        // Set Animation States
        if (isRunning)
        {
            ani.SetBool("isRunAttacking", true);
            ani.SetBool("isWalkAttacking", false);
        }
        else
        {
            ani.SetBool("isRunAttacking", false);
            ani.SetBool("isWalkAttacking", isMoving);
        }

        if (!isMoving)
        {
            ani.SetBool("isWalking", false);
        }

        ani.SetFloat("LookX", attackDirection.x);
        ani.SetFloat("LookY", attackDirection.y);
        ani.SetFloat("LastInputX", attackDirection.x);
        ani.SetFloat("LastInputY", attackDirection.y);

        enemiesHitThisAttack.Clear();
        currentComboCache = combo;

        ani.SetTrigger("Attack");

        // Only increment combo if NOT running (usually run attacks are single hit or break combo chain)
        // Adjust this logic if you want running attacks to be part of the combo
        if (!isRunning)
        {
            combo++;
            if (combo > comboNumber)
            {
                combo = 1;
            }
        }
        else
        {
            // Reset combo for run attack or keep it at 1
            combo = 1;
        }

        comboTempo = 0f;
        comboTiming = 2f;
    }

    public void StartAttack()
    {
        SoundEffectManager.Play("Melee Effect", true);
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
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        // Determine Stance Multiplier
        float stanceMultiplier = 1.0f;

        if (isRunAttacking)
        {
            stanceMultiplier = runAttackMultiplier; // Use run multiplier (e.g. 1.2)
        }
        else if (isWalkAttacking)
        {
            stanceMultiplier = 1.0f; // Standard damage while walking
        }
        else
        {
            stanceMultiplier = 1.5f; // Higher damage for standing still
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemiesHitThisAttack.Contains(enemy))
            {
                continue;
            }

            Enemy enemyChase = enemy.GetComponent<Enemy>();
            if (enemyChase != null)
            {
                float comboScale = checkCombo(currentComboCache);
                // For run attacks, we typically don't use combo scale, or force it to 1
                if (isRunAttacking) comboScale = 1.0f;

                float rawDamage = playerStats.finalPhysicalAttack * comboScale * stanceMultiplier;

                bool isCritical = false;
                float critChance = playerStats.finalCritRate;

                if (UnityEngine.Random.Range(0f, 100f) < critChance)
                {
                    isCritical = true;
                    rawDamage *= 2;
                }

                int finalDamage = Mathf.RoundToInt(rawDamage);

                // Pass the knockback flag if running
                bool forceKnockback = isRunAttacking && causesKnockback;

                enemyChase.TakeDamage(finalDamage, DamageSourceType.Knight, transform.parent, isCritical, forceKnockback);

                enemiesHitThisAttack.Add(enemy);
            }
        }
    }

    private void UpdateAttackPointDirection()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;

        Vector3 direction = (worldMousePos - transform.position).normalized;

        attackDirection = direction;

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
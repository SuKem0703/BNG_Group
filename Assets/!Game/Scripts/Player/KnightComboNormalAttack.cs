using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class KnightComboNormalAttack : MonoBehaviour
{
    private Animator ani;
    public int combo = 1;
    public int comboNumber = 3;

    public float comboTiming = 2f;
    public float comboTempo = 0f;

    private float minComboInterval = 0.5f;
    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();
    private int attackStaminaCost = 1;

    public bool isAttacking => ani.GetBool("isAttacking");

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

        if (ani.GetBool("isAttacking") && !ani.GetBool("isRunAttacking"))
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

        attackPressed = true;
    }

    private void TryAttack()
    {
        if (comboTempo < minComboInterval) return;

        if (!PlayerStats.Instance.CanAttack || !GameStateManager.CanProcessInput())
        {
            return;
        }

        if (playerStats != null && playerStats.currentStamina >= attackStaminaCost)
        {
            Combo();
        }
    }

    public void Combo()
    {
        if (comboTempo < 0)
        {
            Debug.Log("Xuất hiện lỗi! Tempo không thể < 0.");
            return;
        }

        playerStats.UseStamina(attackStaminaCost);
        SoundEffectManager.Play("Melee Effect", true);

        ani.SetBool("isAttacking", true);

        bool isMoving = playerMovement != null && playerMovement.moveInput.magnitude > 0.1f;
        ani.SetBool("isRunAttacking", isMoving);

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

        combo++;
        if (combo > comboNumber)
        {
            combo = 1;
        }

        comboTempo = 0f;
        comboTiming = 2f;
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
        ani.SetBool("isRunAttacking", false);
    }

    public void DealDamageEvent()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        // Kiểm tra trạng thái đang chạy hay đứng im dựa trên Animator
        bool isRunAttacking = ani.GetBool("isRunAttacking");

        // Nếu đứng im (không phải Run Attack) thì nhân 1.5 damage, ngược lại giữ nguyên 1.0
        float stanceMultiplier = isRunAttacking ? 1.0f : 1.5f;

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

                // Áp dụng công thức: BaseAttack * ComboScale * StanceMultiplier
                float rawDamage = playerStats.finalPhysicalAttack * comboScale * stanceMultiplier;

                bool isCritical = false;
                float critChance = playerStats.finalCritRate;

                if (UnityEngine.Random.Range(0f, 100f) < critChance)
                {
                    isCritical = true;
                    rawDamage *= 2;
                    // SoundEffectManager.Play("CriticalHit"); 
                }

                int finalDamage = Mathf.RoundToInt(rawDamage);

                enemyChase.TakeDamage(finalDamage, DamageSourceType.Knight, transform.parent, isCritical);

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
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

        if (ani.GetBool("isAttacking"))
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
            playerStats.UseStamina(attackStaminaCost);
            SoundEffectManager.Play("Melee Effect", true);
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

        ani.SetBool("isAttacking", true);
        ani.SetBool("isWalking", false);

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
    }
    public void DealDamageEvent()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemiesHitThisAttack.Contains(enemy))
            {
                continue;
            }

            Enemy enemyChase = enemy.GetComponent<Enemy>();
            if (enemyChase != null)
            {
                float scale = checkCombo(currentComboCache);
                float rawDamage = playerStats.finalPhysicalAttack * scale;
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

        //attackPoint.position = transform.position + direction * attackRange;

        attackDirection = direction;

        attackPoint.position = transform.position + (Vector3)attackDirection * attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        // 1. Vẽ vùng tấn công (Màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // 2. Vẽ đường hướng tấn công (Màu vàng - Tuỳ chọn)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, attackPoint.position);
    }
}

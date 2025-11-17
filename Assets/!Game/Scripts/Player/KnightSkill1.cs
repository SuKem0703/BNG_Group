using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KnightSkill1 : MonoBehaviour
{
    public float slashSpeed = 10f;
    public float slashDuration = 0.15f;
    public float slashCooldown = 1.5f;
    public int slashStaminaCost = 3;
    public int slashMPCost = 30;

    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    private bool isSlashing = false;
    private float nextSlashTime = 0f;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerStats playerStats;
    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponentInParent<Rigidbody2D>();
        playerStats = GetComponentInParent<PlayerStats>();
    }

    public void OnSlash(InputAction.CallbackContext context)
    {
        if (!context.performed || isSlashing || Time.time < nextSlashTime || PauseController.IsGamePause)
            return;

        if (playerStats != null && playerStats.currentStamina >= slashStaminaCost && playerStats.knightMP >= slashMPCost)
        {
            playerStats.UseStamina(slashStaminaCost);
            playerStats.UseMP(slashMPCost, true);
            StartCoroutine(SlashRoutine());
        }
        else
        {
            Debug.Log("Không đủ stamina để dùng Slash Skill!");
        }
    }

    private IEnumerator SlashRoutine()
    {
        isSlashing = true;
        nextSlashTime = Time.time + slashCooldown;

        // Animation
        animator.SetTrigger("Slash");
        Debug.Log("Slash skill activated!");

        // Tính hướng chuột
        Vector2 direction = ((Vector2)(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())) - rb.position).normalized;

        float elapsed = 0f;

        while (elapsed < slashDuration)
        {
            rb.MovePosition(rb.position + direction * slashSpeed * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        DealDamage(direction);

        isSlashing = false;
    }

    private void DealDamage(Vector2 direction)
    {
        Vector2 center = rb.position + direction.normalized * attackRange * 0.5f;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(center, attackRange, enemyLayer);

        foreach (Collider2D enemy in enemies)
        {
            EnemyChase enemyChase = enemy.GetComponent<EnemyChase>();
            if (enemyChase != null)
            {
                int damage = playerStats != null ? playerStats.finalPhysicalAttack : 1;
                enemyChase.TakeDamage(damage, DamageSourceType.Knight);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}

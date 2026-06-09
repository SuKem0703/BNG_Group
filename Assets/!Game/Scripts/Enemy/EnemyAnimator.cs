using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private Enemy enemyCore;

    void Awake()
    {
        animator = GetComponent<Animator>();
        enemyCore = GetComponent<Enemy>();
    }

    void Update()
    {
        if (PauseController.IsGamePause || enemyCore == null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("IsChasing", false);
            
            animator.SetBool("IsAttacking", false);
            return;
        }

        bool isWalking = enemyCore.netIsWalking.Value;
        bool isChasing = enemyCore.netIsChasing.Value;
        Vector2 dir = enemyCore.netDirection.Value;

        animator.SetBool("isWalking", isWalking);
        animator.SetBool("IsChasing", isChasing);

        if (dir != Vector2.zero)
        {
            animator.SetFloat("InputX", dir.x);
            animator.SetFloat("InputY", dir.y);
            animator.SetFloat("LastInputX", dir.x);
            animator.SetFloat("LastInputY", dir.y);
        }
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction == Vector2.zero) return;
        Vector2 dir = direction.normalized;

        animator.SetFloat("InputX", dir.x);
        animator.SetFloat("InputY", dir.y);
        animator.SetFloat("LastInputX", dir.x);
        animator.SetFloat("LastInputY", dir.y);
    }

    public void TriggerAttack()
    {
        animator.SetBool("IsAttacking", true);
    }

    public void EndAttack()
    {
        animator.SetBool("IsAttacking", false);
        
        if (enemyCore != null)
        {
            enemyCore.isAttacking = false;
        }

        enemyCore.EnemyEndAttack();
    }

    public void TriggerHurt()
    {
        animator.SetTrigger("Hurt");
    }

    public void TriggerDie()
    {
        animator.SetTrigger("isDie");
    }
}
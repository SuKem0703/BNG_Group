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
            return;
        }

        bool isMoving = enemyCore.netIsWalking.Value;
        Vector2 dir = enemyCore.netDirection.Value;

        animator.SetBool("isWalking", isMoving);

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

    public void SetWalking(bool walking)
    {
        if (animator == null) return;
        animator.SetBool("isWalking", walking);
    }

    public void TriggerAttack()
    {
        animator.SetTrigger("Attack");
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
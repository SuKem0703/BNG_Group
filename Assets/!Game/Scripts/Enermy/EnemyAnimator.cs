using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (PauseController.IsGamePause)
        {
            animator.SetBool("isWalking", false);
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        bool isMoving = velocity.magnitude > 0.05f;
        animator.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            animator.SetFloat("InputX", velocity.x);
            animator.SetFloat("InputY", velocity.y);
            animator.SetFloat("LastInputX", velocity.normalized.x);
            animator.SetFloat("LastInputY", velocity.normalized.y);
        }
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        Vector2 dir = direction.normalized;

        // Ưu tiên trục nào lớn hơn
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            // Left / Right
            animator.SetFloat("LastInputX", dir.x > 0 ? 1 : -1);
            animator.SetFloat("LastInputY", 0);
        }
        else
        {
            // Up / Down
            animator.SetFloat("LastInputX", 0);
            animator.SetFloat("LastInputY", dir.y > 0 ? 1 : -1);
        }
    }


    public void SetWalking(bool walking)
    {
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

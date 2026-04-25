using UnityEngine;

public class CharacterFacing : MonoBehaviour
{
    public Animator animator;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void LookTowards(Vector3 targetPosition)
    {
        if (animator == null) return;

        Vector3 lookDirection = (targetPosition - transform.position).normalized;
        animator.SetFloat("LastInputX", lookDirection.x);
        animator.SetFloat("LastInputY", lookDirection.y);
        animator.SetFloat("InputX", 0);
        animator.SetFloat("InputY", 0);
    }
}
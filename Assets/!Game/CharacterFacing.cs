using UnityEngine;

public enum FacingDirection
{
    down,
    up,
    left,
    right
}

public class NPCAnimation : MonoBehaviour
{
    public Animator animator;

    public FacingDirection initialFacing = FacingDirection.down;

    // Cấu hình chớp mắt
public bool enableBlink = true;
    public float minBlinkInterval = 4f;
    public float maxBlinkInterval = 10f;

    private float blinkTimer;
    private bool hasBlinkParameter;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == "blink" && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasBlinkParameter = true;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        // Khởi tạo thời gian đếm ngược chớp mắt
        if (enableBlink)
        {
            blinkTimer = Random.Range(minBlinkInterval, maxBlinkInterval);
        }

        if (animator == null || initialFacing == FacingDirection.down) return;

        switch (initialFacing)
        {
            case FacingDirection.down:
                animator.SetFloat("LastInputX", 0);
                animator.SetFloat("LastInputY", -1);
                break;
            case FacingDirection.up:
                animator.SetFloat("LastInputX", 0);
                animator.SetFloat("LastInputY", 1);
                break;
            case FacingDirection.left:
                animator.SetFloat("LastInputX", -1);
                animator.SetFloat("LastInputY", 0);
                break;
            case FacingDirection.right:
                animator.SetFloat("LastInputX", 1);
                animator.SetFloat("LastInputY", 0);
                break;
        }
    }

    // Xử lý đếm thời gian và gọi trigger chớp mắt
    private void Update()
    {
        if (!enableBlink || animator == null || !hasBlinkParameter) return;

        if (animator.GetFloat("LastInputY") > 0.1f) return;

        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            animator.SetTrigger("blink");
            blinkTimer = Random.Range(minBlinkInterval, maxBlinkInterval);
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
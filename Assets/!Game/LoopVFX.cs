using UnityEngine;

public class LoopVFX : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private PlayerCore target;

    private static readonly int LastInputX = Animator.StringToHash("LastInputX");
    private static readonly int LastInputY = Animator.StringToHash("LastInputY");

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    public void Initialize(PlayerCore player, float duration)
    {
        target = player;
        if (duration > 0f) Destroy(gameObject, duration);
        UpdateDirection();
    }

    private void LateUpdate() => UpdateDirection();

    private void UpdateDirection()
    {
        if (target == null || target.playerMovement == null || animator == null) return;

        Vector2 facing = target.playerMovement.netLastInput.Value;
        if (facing.sqrMagnitude < 0.01f) facing = Vector2.down;
        facing = Mathf.Abs(facing.x) > Mathf.Abs(facing.y)
            ? (facing.x >= 0f ? Vector2.right : Vector2.left)
            : (facing.y >= 0f ? Vector2.up : Vector2.down);

        animator.SetFloat(LastInputX, facing.x);
        animator.SetFloat(LastInputY, facing.y);
    }
}
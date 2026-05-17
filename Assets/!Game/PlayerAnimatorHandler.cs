using Unity.Netcode;
using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private PlayerMovement playerMovement;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        if (playerStats == null) return;

        playerStats.netIsDead.OnValueChanged += OnDeathStateChanged;

        if (playerStats.netIsDead.Value && !DeathService.IsRespawningFlag)
        {
            ApplyDeathVisuals();
        }
    }

    private void Update()
    {
        if (playerMovement == null || playerStats == null || animator == null) return;

        Vector2 currentMove = playerMovement.IsOwner ? playerMovement.moveInput : playerMovement.netMoveInput.Value;
        bool currentRun = playerMovement.IsOwner ? playerMovement.isRunning : playerMovement.netIsRunning.Value;
        Vector2 currentLast = playerMovement.netLastInput.Value;

        bool canMove = !playerMovement.isAttacking || playerMovement.canMoveWhileAttacking;

        bool isMoving = currentMove.magnitude > 0.1f && canMove;

        animator.SetBool("isWalking", isMoving && !currentRun);
        animator.SetBool("isRunning", isMoving && currentRun);

        if (isMoving)
        {
            animator.SetFloat("InputX", currentMove.x);
            animator.SetFloat("InputY", currentMove.y);
        }

        animator.SetFloat("LastInputX", currentLast.x);
        animator.SetFloat("LastInputY", currentLast.y);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.netIsDead.OnValueChanged -= OnDeathStateChanged;
        }
    }

    private void OnDeathStateChanged(bool previousValue, bool newValue)
    {
        if (newValue) ApplyDeathVisuals();
        else ResetVisuals();
    }

    private void ApplyDeathVisuals()
    {
        animator.SetTrigger("Die");
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
    }

    private void ResetVisuals()
    {
        animator.ResetTrigger("Die");
        animator.Play("Idle");
    }

    public void OnDeathAnimationFinished()
    {
        if (playerStats != null && playerStats.IsOwner && !DeathService.IsRespawningFlag)
        {
            playerStats.OnCharacterDeathAnimationFinished();
        }
    }
}
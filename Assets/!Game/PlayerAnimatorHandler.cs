using Unity.Netcode;
using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private PlayerMovement playerMovement;

    private void Awake()
    {
        animator = animator ?? GetComponent<Animator>();
        playerVitals = playerVitals ?? GetComponentInParent<PlayerVitals>();
        playerMovement = playerMovement ?? GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        if (playerVitals == null) return;

        playerVitals.netIsDead.OnValueChanged += OnDeathStateChanged;

        if (playerVitals.netIsDead.Value && !DeathService.IsRespawningFlag)
        {
            ApplyDeathVisuals();
        }
    }

    private void Update()
    {
        if (playerMovement == null || playerVitals == null || animator == null) return;

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
        if (playerVitals != null)
        {
            playerVitals.netIsDead.OnValueChanged -= OnDeathStateChanged;
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
        if (playerVitals != null && playerVitals.IsOwner && !DeathService.IsRespawningFlag)
        {
            playerVitals.OnCharacterDeathAnimationFinished();
        }
    }
}
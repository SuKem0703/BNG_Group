using Unity.Netcode;
using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Idle Look Settings")]
    [SerializeField] private bool enableIdleLook = true;
    [SerializeField] private float minIdleTime = 5f;
    [SerializeField] private float maxIdleTime = 10f;
    private float idleTimer;
    private bool hasIdleLookParameter;

    [Header("Idle Blink Settings")]
    [SerializeField] private bool enableIdleBlink = true;
    [SerializeField] private float minBlinkTime = 2f;
    [SerializeField] private float maxBlinkTime = 4f;
    private float blinkTimer;
    private bool hasBlinkParameter;

    private void Awake()
    {
        animator = animator ?? GetComponent<Animator>();
        playerVitals = playerVitals ?? GetComponentInParent<PlayerVitals>();
        playerMovement = playerMovement ?? GetComponentInParent<PlayerMovement>();

        if (animator != null)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == "IdleLook" && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasIdleLookParameter = true;
                }
                else if (parameter.name == "Blink" && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasBlinkParameter = true;
                }
            }
        }
    }

    private void Start()
    {
        if (playerVitals == null) return;

        playerVitals.netIsDead.OnValueChanged += OnDeathStateChanged;

        if (playerVitals.netIsDead.Value && !DeathService.IsRespawningFlag)
        {
            ApplyDeathVisuals();
        }

        ResetIdleTimer();
        ResetBlinkTimer();
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

        bool isTrulyIdle = !isMoving && !playerMovement.isAttacking && !playerVitals.netIsDead.Value;

        if (isTrulyIdle)
        {
            if (enableIdleLook && hasIdleLookParameter)
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    animator.SetTrigger("IdleLook");
                    ResetIdleTimer();
                }
            }

            if (enableIdleBlink && hasBlinkParameter && currentLast.y <= 0.1f)
            {
                blinkTimer -= Time.deltaTime;
                if (blinkTimer <= 0f)
                {
                    animator.SetTrigger("Blink");
                    ResetBlinkTimer();
                }
            }
        }
        else
        {
            ResetIdleTimer();
            ResetBlinkTimer();
        }
    }

    private void ResetIdleTimer()
    {
        idleTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void ResetBlinkTimer()
    {
        blinkTimer = Random.Range(minBlinkTime, maxBlinkTime);
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
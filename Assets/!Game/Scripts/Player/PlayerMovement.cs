using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed;

    [Header("Dash Settings")]
    [SerializeField] public float dashSpeed = 12f;
    [SerializeField] public float dashDuration = 0.2f;
    [SerializeField] public float dashCooldown = 1f;
    [SerializeField] public int dashStaminaCost = 2;

    [Header("Run (Sprint) Settings")]
    [SerializeField] public float runSpeedMultiplier = 1.5f;
    [SerializeField] public float runStaminaCostPerSec = 10f;
    [SerializeField] public float runHoldThreshold = 0.5f;

    public Rigidbody2D rb;
    public Vector2 moveInput;
    public Animator animator;

    public bool isDashing = false;
    public bool isRunning = false;
    private bool isDashOnCooldown = false;

    private bool isDashButtonHeld = false;
    private bool isSprintLocked = false;
    private float holdTimer = 0f;
    private bool canRunAfterDash = false;

    private float staminaDrainTimer = 0f;

    private bool isDead = false;
    public bool IsDead => isDead;

    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    public KnightComboNormalAttack comboAttack;

    public GhostTrail ghostTrail;

    public NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> netLastInput = new NetworkVariable<Vector2>(new Vector2(0, -1), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> netIsRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (comboAttack == null) comboAttack = GetComponentInChildren<KnightComboNormalAttack>();
        if (ghostTrail == null) ghostTrail = GetComponentInChildren<GhostTrail>();
    }

    void Update()
    {
        if (IsOwner)
        {
            if (isDead || !GameStateManager.CanProcessInput() || !SaveController.IsDataLoaded)
            {
                ResetMovementState();
            }
            else
            {
                bool isMovingOwner = moveInput.magnitude > 0.1f;

                if (isDashButtonHeld && canRunAfterDash)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= runHoldThreshold) isSprintLocked = true;
                }
                else if (!isDashButtonHeld && !isSprintLocked)
                {
                    holdTimer = 0f;
                }

                bool intentionToRun = (isDashButtonHeld && canRunAfterDash) || isSprintLocked;

                if (intentionToRun && isMovingOwner && !isDashing && playerStats.currentStamina > 0)
                {
                    isRunning = true;
                    HandleRunStamina();
                }
                else
                {
                    isRunning = false;
                    staminaDrainTimer = 0f;

                    if (!isMovingOwner || playerStats.currentStamina <= 0)
                    {
                        isSprintLocked = false;
                        if (playerStats.currentStamina <= 0) canRunAfterDash = false;
                    }
                }

                if (!isDashing)
                {
                    bool isCurrentlyAttacking = animator.GetBool("isAttacking");
                    bool isWalkAttacking = animator.GetBool("isWalkAttacking");
                    bool isRunAttacking = animator.GetBool("isRunAttacking");
                    bool canMove = !isCurrentlyAttacking || isWalkAttacking || isRunAttacking;

                    float currentSpeed = isRunning ? (moveSpeed * runSpeedMultiplier) : moveSpeed;
                    rb.linearVelocity = canMove ? moveInput * currentSpeed : Vector2.zero;
                }

                netMoveInput.Value = moveInput;
                netIsRunning.Value = isRunning;
            }
        }

        Vector2 currentMove = IsOwner ? moveInput : netMoveInput.Value;
        bool currentRun = IsOwner ? isRunning : netIsRunning.Value;
        Vector2 currentLast = netLastInput.Value;

        bool isMoving = currentMove.magnitude > 0.1f;

        animator.SetBool("isWalking", isMoving && !currentRun);
        animator.SetBool("isRunning", isMoving && currentRun);

        if (isMoving)
        {
            animator.SetFloat("InputX", currentMove.x);
            animator.SetFloat("InputY", currentMove.y);
        }

        animator.SetFloat("LastInputX", currentLast.x);
        animator.SetFloat("LastInputY", currentLast.y);

        if (playerStats != null)
        {
            moveSpeed = playerStats.finalMoveSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (isDead) { rb.linearVelocity = Vector2.zero; return; }
        bool isWalkAttacking = animator.GetBool("isWalkAttacking");
        bool isRunAttacking = animator.GetBool("isRunAttacking");
        bool attackAllowsMovement = isWalkAttacking || isRunAttacking;
        if (PauseController.IsGamePause || isDashing || (!attackAllowsMovement && comboAttack != null && comboAttack.isAttacking) || !SaveController.IsDataLoaded) return;

        float currentSpeed = isRunning ? (moveSpeed * runSpeedMultiplier) : moveSpeed;
        rb.linearVelocity = moveInput * currentSpeed;
    }

    private void OnEnable()
    {
        isDashing = false;
        isRunning = false;
        isDashOnCooldown = false;
        isDashButtonHeld = false;
        isSprintLocked = false;
        canRunAfterDash = false;
        isDead = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (IsOwner)
            {
                // Nếu đây là Host: Lấy vị trí từ SaveController đã được cache ở RelayManager
                if (SaveController.nextSpawnPosition.HasValue)
                {
                    transform.position = SaveController.nextSpawnPosition.Value;
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
                // Nếu Server đang xử lý object của Client: Đặt nó cạnh Host
                var hostObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkManager.ServerClientId);
                if (hostObj != null)
                {
                    transform.position = hostObj.transform.position;
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                }
            }
        }

        if (IsOwner && !IsServer)
        {
            // Client tự động cập nhật vị trí của chính nó để khớp với Host ở local
            var hostObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkManager.ServerClientId);
            if (hostObj != null)
            {
                transform.position = hostObj.transform.position;
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        if (IsOwner)
        {
            var input = GetComponentInChildren<PlayerInput>(true);
            if (input != null) input.enabled = true;

            if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;
        }
        else
        {
            var inputs = GetComponentsInChildren<PlayerInput>(true);
            foreach (var input in inputs) input.enabled = false;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!IsOwner || isDead) return;

        Vector2 rawInput = context.ReadValue<Vector2>();
        moveInput = PauseController.IsGamePause ? Vector2.zero : rawInput;

        if (moveInput.magnitude > 0.01f)
        {
            netLastInput.Value = moveInput;
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!IsOwner || isDead || PauseController.IsGamePause) return;

        if (context.started)
        {
            isDashButtonHeld = true;
            holdTimer = 0f;
            canRunAfterDash = false;
        }
        else if (context.canceled)
        {
            isDashButtonHeld = false;
            holdTimer = 0f;
            if (!isSprintLocked) canRunAfterDash = false;
        }

        if (context.performed && !isDashing && !isDashOnCooldown && moveInput != Vector2.zero)
        {
            if (playerStats != null && playerStats.currentStamina >= dashStaminaCost)
            {
                playerStats.UseStamina(dashStaminaCost);
                StartCoroutine(DashRoutine());
            }
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        isDashOnCooldown = true;
        isRunning = false;

        playerStats?.SetInvincible(true);
        SoundEffectManager.Play("Dash", true);

        ghostTrail?.CreateTrail();

        float elapsed = 0f;
        Vector2 dashDirection = moveInput.normalized;

        while (elapsed < dashDuration)
        {
            rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        playerStats?.SetInvincible(false);

        if (isDashButtonHeld)
        {
            canRunAfterDash = true;
        }

        yield return new WaitForSeconds(dashCooldown);
        isDashOnCooldown = false;
    }

    public void LookTowards(Vector3 targetPosition)
    {
        if (!IsOwner || isDead) return;

        Vector3 lookDirection = (targetPosition - transform.position).normalized;

        netLastInput.Value = new Vector2(lookDirection.x, lookDirection.y);

        moveInput = Vector2.zero;
        netMoveInput.Value = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;
        ResetMovementState();

        animator.SetBool("isAttacking", false);
        animator.SetBool("isWalkAttacking", false);
        animator.ResetTrigger("Attack");

        try { animator.Play("Die", -1, 0f); } catch { animator.SetTrigger("Die"); }
    }

    public void TriggerDeathUI()
    {
        if (!IsOwner) return;
        GameOverUIAdapter.Instance.ShowGameOverUI();
    }

    private void HandleRunStamina()
    {
        if (playerStats == null) return;
        staminaDrainTimer += Time.deltaTime;
        float timePerPoint = 1f / runStaminaCostPerSec;

        if (staminaDrainTimer >= timePerPoint)
        {
            playerStats.UseStamina(0.1f);
            staminaDrainTimer = 0f;
        }
    }

    private void ResetMovementState()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        isSprintLocked = false;
        isDashButtonHeld = false;
        canRunAfterDash = false;
        holdTimer = 0f;

        if (IsOwner)
        {
            netMoveInput.Value = Vector2.zero;
            netIsRunning.Value = false;
        }
    }
}
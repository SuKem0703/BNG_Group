using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed;

    [Header("Dash Settings")]
    [SerializeField] public float dashSpeed = 12f;
    [SerializeField] public float dashDuration = 0.2f;
    [SerializeField] public float dashCooldown = 1f;
    [SerializeField] public int dashStaminaCost = 3;

    [Header("Run (Sprint) Settings")]
    [SerializeField] public float runSpeedMultiplier = 1.5f;
    [SerializeField] public float runStaminaCostPerSec = 10f;
    [SerializeField] public float runHoldThreshold = 0.5f;

    private Rigidbody2D rb;
    public Vector2 moveInput;
    public Animator animator;

    // States
    public bool isDashing = false;
    public bool isRunning = false;
    private bool isDashOnCooldown = false;

    // Logic Input & Lock
    private bool isDashButtonHeld = false;
    private bool isSprintLocked = false;
    private float holdTimer = 0f;

    private bool canRunAfterDash = false;

    // Stamina Timer
    private float staminaDrainTimer = 0f;

    // Death State
    private bool isDead = false;
    public bool IsDead => isDead;

    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private KnightComboNormalAttack comboAttack;

    void Awake()
    {
        comboAttack = GetComponentInChildren<KnightComboNormalAttack>();
        rb = GetComponentInParent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead || !GameStateManager.CanProcessInput() || !SaveController.IsDataLoaded)
        {
            ResetMovementState();
            return;
        }

        bool isMoving = moveInput.magnitude > 0.1f;

        if (isDashButtonHeld && canRunAfterDash)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= runHoldThreshold)
            {
                isSprintLocked = true;
            }
        }
        else if (!isDashButtonHeld && !isSprintLocked)
        {
            holdTimer = 0f;
        }

        bool intentionToRun = (isDashButtonHeld && canRunAfterDash) || isSprintLocked;

        if (intentionToRun && isMoving && !isDashing && playerStats.currentStamina > 0)
        {
            isRunning = true;
            HandleRunStamina();
        }
        else
        {
            isRunning = false;
            staminaDrainTimer = 0f;

            // Hủy khóa Sprint nếu dừng di chuyển hoặc hết Stamina
            if (!isMoving || playerStats.currentStamina <= 0)
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
            // Allow movement during walk-attacking or run-attacking
            bool canMove = !isCurrentlyAttacking || isWalkAttacking || isRunAttacking;

            float currentSpeed = isRunning ? (moveSpeed * runSpeedMultiplier) : moveSpeed;
            rb.linearVelocity = canMove ? moveInput * currentSpeed : Vector2.zero;
        }

        animator.SetBool("isWalking", isMoving && !isRunning);
        animator.SetBool("isRunning", isMoving && isRunning);

        if (playerStats != null)
        {
            moveSpeed = playerStats.finalMoveSpeed;
        }
    }

    private Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
            return color;
        return Color.white;
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

    private void FixedUpdate()
    {
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

    public void Move(InputAction.CallbackContext context)
    {
        if (isDead) return;
        Vector2 rawInput = context.ReadValue<Vector2>();
        moveInput = PauseController.IsGamePause ? Vector2.zero : rawInput;

        if (moveInput.magnitude > 0.01f)
        {
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (isDead || PauseController.IsGamePause) return;

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
            // Thả tay ra thì reset quyền chạy (trừ khi đã lock sprint)
            if (!isSprintLocked) canRunAfterDash = false;
        }

        // Logic kích hoạt Dash
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

        // Đang Dash thì không tính là Run
        isRunning = false;

        playerStats?.SetInvincible(true);
        SoundEffectManager.Play("Dash", true);
        GetComponent<GhostTrail>()?.CreateTrail();

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

        // Nếu vẫn đang giữ nút Shift -> Mới cấp quyền cho phép chạy (canRunAfterDash = true)
        if (isDashButtonHeld)
        {
            canRunAfterDash = true;
        }

        yield return new WaitForSeconds(dashCooldown);
        isDashOnCooldown = false;
    }

    public void LookTowards(Vector3 targetPosition)
    {
        if (isDead) return;
        Vector3 lookDirection = (targetPosition - transform.position).normalized;
        animator.SetFloat("LastInputX", lookDirection.x);
        animator.SetFloat("LastInputY", lookDirection.y);
        animator.SetFloat("InputX", 0);
        animator.SetFloat("InputY", 0);
        moveInput = Vector2.zero;
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
        DeathManager.Instance.ShowGameOverUI();
    }
}
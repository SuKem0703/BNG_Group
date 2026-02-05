using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float moveSpeed;
    [SerializeField] public float dashSpeed = 12f;
    [SerializeField] public float dashDuration = 0.2f;
    [SerializeField] public float dashCooldown = 1f;
    [SerializeField] public int dashStaminaCost = 5;

    private Rigidbody2D rb;
    public Vector2 moveInput;
    public Animator animator;
    public bool isDashing = false;
    private bool isDashOnCooldown = false;
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
        if (!GameStateManager.CanProcessInput() || !SaveController.IsDataLoaded)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            animator.SetBool("isAttacking", false);
            animator.SetBool("isRunAttacking", false); // Reset trạng thái này khi game dừng
            return;
        }

        bool isCurrentlyAttacking = animator.GetBool("isAttacking");
        // Kiểm tra xem có đang thực hiện đòn tấn công vừa chạy vừa đánh không
        bool isRunAttacking = animator.GetBool("isRunAttacking");

        if (!isDashing)
        {
            // Cho phép di chuyển nếu KHÔNG tấn công HOẶC đang thực hiện Run Attack
            bool canMove = !isCurrentlyAttacking || isRunAttacking;
            rb.linearVelocity = canMove ? moveInput * moveSpeed : Vector2.zero;
        }

        animator.SetBool("isWalking", rb.linearVelocity.magnitude > 0.1f);

        if (playerStats != null)
        {
            moveSpeed = playerStats.finalMoveSpeed;
        }
    }
    private void FixedUpdate()
    {
        // Cho phép physics cập nhật nếu đang Run Attack
        bool isRunAttacking = animator.GetBool("isRunAttacking");

        if (PauseController.IsGamePause || isDashing || (!isRunAttacking && comboAttack != null && comboAttack.isAttacking) || !SaveController.IsDataLoaded)
            return;

        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void OnEnable()
    {
        isDashing = false;
        isDashOnCooldown = false;
    }
    public void Move(InputAction.CallbackContext context)
    {
        Vector2 rawInput = context.ReadValue<Vector2>();

        if (PauseController.IsGamePause)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = rawInput;
        }

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

        if (moveInput.magnitude > 0.1f)
        {
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (PauseController.IsGamePause || !context.performed || isDashing || isDashOnCooldown || moveInput == Vector2.zero)
            return;

        if (playerStats != null && playerStats.currentStamina >= dashStaminaCost)
        {
            playerStats.UseStamina(dashStaminaCost);
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        isDashOnCooldown = true;

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

        yield return new WaitForSeconds(dashCooldown);
        isDashOnCooldown = false;
    }

    public void LookTowards(Vector3 targetPosition)
    {
        Vector3 lookDirection = (targetPosition - transform.position).normalized;

        animator.SetFloat("LastInputX", lookDirection.x);
        animator.SetFloat("LastInputY", lookDirection.y);

        animator.SetFloat("InputX", 0);
        animator.SetFloat("InputY", 0);

        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }
}
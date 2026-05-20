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

    public bool isAttacking = false;
    public bool canMoveWhileAttacking = false;

    [SerializeField] private PlayerCore core;
    public GhostTrail ghostTrail;

    public NetworkVariable<Vector2> netMoveInput = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<Vector2> netLastInput = new NetworkVariable<Vector2>(new Vector2(0, -1), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> netIsRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Update()
    {
        if (IsOwner)
        {
            bool isGameEnded = core.playerStats != null && core.playerVitals.isGameOver;

            if (isDead || isGameEnded || !GameStateManager.CanProcessInput() || !SaveController.IsDataLoaded)
            {
                ResetMovementState();

                if (isGameEnded && rb != null) rb.linearVelocity = Vector2.zero;
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

                if (intentionToRun && isMovingOwner && !isDashing && core.playerVitals.currentStamina > 0)
                {
                    isRunning = true;
                    HandleRunStamina();
                }
                else
                {
                    isRunning = false;
                    staminaDrainTimer = 0f;

                    if (!isMovingOwner || core.playerVitals.currentStamina <= 0)
                    {
                        isSprintLocked = false;
                        if (core.playerVitals.currentStamina <= 0) canRunAfterDash = false;
                    }
                }

                if (!isDashing)
                {
                    bool canMove = !isAttacking || canMoveWhileAttacking;
                    float currentSpeed = isRunning ? (moveSpeed * runSpeedMultiplier) : moveSpeed;
                    rb.linearVelocity = canMove ? moveInput * currentSpeed : Vector2.zero;
                }

                netMoveInput.Value = moveInput;
                netIsRunning.Value = isRunning;
            }
        }

        if (core.playerStats != null) moveSpeed = core.playerStats.finalMoveSpeed;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        bool isGameEnded = core.playerStats != null && core.playerVitals.isGameOver;

        if (isDead || isGameEnded)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        bool attackAllowsMovement = canMoveWhileAttacking;
        if (PauseController.IsGamePause || isDashing || (!attackAllowsMovement && isAttacking) || !SaveController.IsDataLoaded) return;

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
        isAttacking = false;
        canMoveWhileAttacking = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (IsOwner)
            {
                if (SaveController.nextSpawnPosition.HasValue)
                {
                    transform.position = SaveController.nextSpawnPosition.Value;
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                }
            }
            else
            {
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
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (obj.IsPlayerObject && obj.OwnerClientId == NetworkManager.ServerClientId)
                {
                    transform.position = obj.transform.position;
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    break;
                }
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
        if (!IsOwner) return;

        if (core.playerStats != null && core.playerVitals.isGameOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 rawInput = context.ReadValue<Vector2>();
        moveInput = (PauseController.IsGamePause || isDead) ? Vector2.zero : rawInput;

        if (moveInput.magnitude > 0.01f)
        {
            netLastInput.Value = moveInput;
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

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

        bool isGameEnded = core.playerStats != null && core.playerVitals.isGameOver;
        if (isDead || isGameEnded || PauseController.IsGamePause) return;

        if (isAttacking && !canMoveWhileAttacking) return;

        if (context.performed && !isDashing && !isDashOnCooldown && moveInput != Vector2.zero)
        {
            if (core.playerStats != null && core.playerVitals.currentStamina >= dashStaminaCost)
            {
                core.playerVitals.UseStamina(dashStaminaCost);
                StartCoroutine(DashRoutine());
            }
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        isDashOnCooldown = true;
        isRunning = false;

        core.playerVitals?.SetInvincible(true);
        SoundEffectManager.Play("Dash", true);

        ghostTrail?.CreateTrail();

        float elapsed = 0f;
        Vector2 dashDirection = moveInput.normalized;

        while (elapsed < dashDuration)
        {
            bool isGameEnded = core.playerStats != null && core.playerVitals.isGameOver;
            if (isDead || isGameEnded) break;

            rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
        if (!isDead) core.playerVitals?.SetInvincible(false);

        if (isDashButtonHeld) canRunAfterDash = true;

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
    }

    public void TriggerDeathUI()
    {
        if (!IsOwner) return;
        GameOverUIAdapter.Instance.ShowGameOverUI();
    }

    private void HandleRunStamina()
    {
        if (core.playerStats == null) return;
        staminaDrainTimer += Time.deltaTime;
        float timePerPoint = 1f / runStaminaCostPerSec;

        if (staminaDrainTimer >= timePerPoint)
        {
            core.playerVitals.UseStamina(0.1f);
            staminaDrainTimer = 0f;
        }
    }

    private void ResetMovementState()
    {
        rb.linearVelocity = Vector2.zero;
        isSprintLocked = false;
        isDashButtonHeld = false;
        canRunAfterDash = false;
        holdTimer = 0f;
        isAttacking = false;
        canMoveWhileAttacking = false;
        moveInput = Vector2.zero;

        if (IsOwner)
        {
            netMoveInput.Value = Vector2.zero;
            netIsRunning.Value = false;
        }
    }

    public void ResetDeathState()
    {
        isDead = false;
        ResetMovementState();
    }
}
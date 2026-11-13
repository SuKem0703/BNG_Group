
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class KnightNormalAttack : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCore core;
    [SerializeField] private CombatTargetSelector targetSelector;

    [Header("Combo Settings")]
    public int combo = 1;
    public int comboNumber = 3;
    public float comboTiming = 2f;
    public float comboTempo = 0f;

    [SerializeField]
    private float minComboInterval = 0.5f;

    [Header("Attack Settings")]
    public int runAttackStaminaCost = 1;
    public int normalAttackStaminaCost = 0;

    public float runAttackMultiplier = 1.2f;
    public bool causesKnockback = true;

    [Range(0f, 360f)]
    public float attackAngle = 180f;

    [Header("Attack Detection")]
    public Transform attackPoint;
    public float attackRange = 0.6f;
    public LayerMask enemyLayer;

    [Header("Hit Feedback")]
    public float hitShakeIntensity = 1.5f;
    public float hitShakeFrequency = 2f;
    public float hitShakeDuration = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugAttackEvents = false;

    [Header("Attack Animation Recovery")]
    [Tooltip("So giay cho Animator vao Attack State. De phat hien transition dau vao bi ket.")]
    [SerializeField, Min(0.1f)] private float attackStateEnterTimeout = 1.0f;

    [Tooltip("Gioi han an toan trong truong hop Animator bi ngat/khong cap nhat.")]
    [SerializeField, Min(0.5f)] private float maxAttackDuration = 3f;

    private float attackStartedAt;
    private bool attackStateSeen;

    // Ten states dung theo Animator Controller dang su dung.
    private static readonly int NormalAttackStateHash =
        Animator.StringToHash("Normal Attack");
    private static readonly int WalkAttackStateHash =
        Animator.StringToHash("Walk Attack");
    private static readonly int RunAttackStateHash =
        Animator.StringToHash("Run Attack");
    private static readonly int IdleGroupStateHash =
        Animator.StringToHash("Base Layer.Idle_Group");

    // Animator States
    public bool isAttacking =>
        animator != null &&
        animator.GetBool("isAttacking");

    public bool isWalking =>
        animator != null &&
        animator.GetBool("isWalking");

    public bool isRunning =>
        animator != null &&
        animator.GetBool("isRunning");

    public bool isWalkAttacking =>
        animator != null &&
        animator.GetBool("isWalkAttacking");

    public bool isRunAttacking =>
        animator != null &&
        animator.GetBool("isRunAttacking");

    // Attack State
    private bool attackPressed;
    private bool attackActive;
    private bool damageProcessedThisAttack;

    private int currentComboCache = 1;
    private int attackSequence;

    private Vector2 attackDirection = Vector2.down;

    // Mỗi Enemy chỉ trúng một lần mỗi đòn.
    private readonly HashSet<Enemy> damagedEnemies =
        new HashSet<Enemy>();

    private int enemyHurtboxLayer = -1;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (core == null)
            core = GetComponentInParent<PlayerCore>();

        enemyHurtboxLayer =
            LayerMask.NameToLayer("EnemyHurtbox");

        if (enemyHurtboxLayer < 0)
        {
            Debug.LogError(
                "[KnightNormalAttack] " +
                "Layer EnemyHurtbox chưa tồn tại!"
            );
        }
    }

    private void Start()
    {
        comboTempo = comboTiming;
        combo = 1;

        // Không ghi đè comboNumber từ Inspector.
        comboNumber = Mathf.Max(1, comboNumber);

        if (enemyHurtboxLayer >= 0)
        {
            enemyLayer = 1 << enemyHurtboxLayer;
        }

        if (attackPoint == null)
        {
            Debug.LogError(
                "[KnightNormalAttack] AttackPoint chưa được gán!"
            );
        }
    }

    // Theo doi animation sau khi Animator da duoc evaluate trong frame nay.
    // Chay tren ca owner va remote client, vi ClientRpc cung dat attackActive.
    private void LateUpdate()
    {
        WatchAttackAnimation();
    }

    private static bool IsAttackState(AnimatorStateInfo state)
    {
        int hash = state.shortNameHash;
        return hash == NormalAttackStateHash ||
               hash == WalkAttackStateHash ||
               hash == RunAttackStateHash;
    }

    private bool IsAttackPlayingOrEntering()
    {
        if (animator == null) return false;
        if (IsAttackState(animator.GetCurrentAnimatorStateInfo(0)))
            return true;

        return animator.IsInTransition(0) &&
               IsAttackState(animator.GetNextAnimatorStateInfo(0));
    }

    private void WatchAttackAnimation()
    {
        if (!attackActive || animator == null || !animator.isActiveAndEnabled)
            return;

        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool currentIsAttack = IsAttackState(currentState);
        bool transitioning = animator.IsInTransition(0);
        bool nextIsAttack = transitioning &&
            IsAttackState(animator.GetNextAnimatorStateInfo(0));

        if (currentIsAttack || nextIsAttack)
            attackStateSeen = true;

        float elapsed = Time.time - attackStartedAt;

        // Animator da roi khoi Attack State (ke ca vi bi Hurt/Die ngat).
        // Event co bi bo qua thi trang thai combat van duoc mo khoa.
        if (attackStateSeen && !currentIsAttack && !nextIsAttack)
        {
            FinishAttack("Animator left Attack State");
            return;
        }

        // Clip da di het mot chu ky nhung Animator van o Attack State:
        // thuong do transition cho mot Bool khong con thoa man.
        if (currentIsAttack && !transitioning && currentState.normalizedTime >= 1.02f)
        {
            FinishAttack("Animation finished but EndAttack event was missed");
            ForceReturnToLocomotion();
            return;
        }

        // Fallback trong truong hop state khong vao duoc/Animator khong tien trien.
        if ((!attackStateSeen && elapsed >= attackStateEnterTimeout) ||
            elapsed >= maxAttackDuration)
        {
            FinishAttack(attackStateSeen ? "Attack timeout" : "Attack state never entered");
            if (currentIsAttack && !transitioning)
                ForceReturnToLocomotion();
        }
    }

    private void ForceReturnToLocomotion()
    {
        if (animator != null && animator.HasState(0, IdleGroupStateHash))
            animator.CrossFadeInFixedTime(IdleGroupStateHash, 0f, 0, 0f);
        else
            Debug.LogWarning("[KnightNormalAttack] Khong tim thay Base Layer.Idle_Group de thoat Attack State.", this);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (core == null || animator == null)
            return;

        UpdateAttackPointDirection();

        if (PauseController.IsGamePause)
        {
            CancelAttack();
            return;
        }

        comboTempo += Time.deltaTime;

        if (comboTiming > 0f)
        {
            comboTiming -= Time.deltaTime;

            if (comboTiming <= 0f)
                combo = 1;
        }

        if (isAttacking &&
            !isWalkAttacking &&
            !isRunAttacking)
        {
            animator.SetBool("isWalking", false);
        }

        if (!attackPressed)
            return;

        attackPressed = false;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TryAttack();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;

        if (!context.performed ||
            PauseController.IsGamePause)
        {
            return;
        }

        if (core == null ||
            core.playerMovement == null ||
            core.playerMovement.IsDead)
        {
            return;
        }

        attackPressed = true;
    }

    private void TryAttack()
    {
        if (attackActive || isAttacking || IsAttackPlayingOrEntering())
            return;

        if (comboTempo < minComboInterval)
            return;

        if (core.playerMovement != null &&
            core.playerMovement.IsDead)
        {
            return;
        }

        if (core.playerStats == null ||
            core.playerVitals == null)
        {
            return;
        }

        if (!core.playerStats.CanAttack ||
            !GameStateManager.CanProcessInput())
        {
            return;
        }

        bool running =
            core.playerMovement != null &&
            core.playerMovement.isRunning;

        int staminaCost = running
            ? runAttackStaminaCost
            : normalAttackStaminaCost;

        if (core.playerVitals.currentStamina < staminaCost)
            return;

        Combo(staminaCost);
    }

    public void Combo(int staminaCost)
    {
        // Chỉ Owner được tạo lượt đánh.
        if (!IsOwner || attackActive || IsAttackPlayingOrEntering())
            return;

        if (core == null ||
            core.playerVitals == null ||
            core.playerStats == null ||
            core.playerMovement == null ||
            animator == null)
        {
            return;
        }

        if (comboTempo < 0f)
            return;

        core.playerVitals.UseStamina(staminaCost);

        bool isMoving =
            core.playerMovement.moveInput.magnitude > 0.1f;

        bool isRunningLocal =
            core.playerMovement.isRunning;

        // Khởi tạo lượt tấn công mới.
        attackSequence++;
        attackActive = true;
        attackStartedAt = Time.time;
        attackStateSeen = false;
        damageProcessedThisAttack = false;

        damagedEnemies.Clear();

        currentComboCache = combo;

        animator.SetBool("isAttacking", true);

        core.playerMovement.isAttacking = true;

        core.playerMovement.canMoveWhileAttacking =
            isRunningLocal || isMoving;

        if (isRunningLocal)
        {
            animator.SetBool("isRunAttacking", true);
            animator.SetBool("isWalkAttacking", false);
        }
        else
        {
            animator.SetBool("isRunAttacking", false);
            animator.SetBool("isWalkAttacking", isMoving);
        }

        if (!isMoving)
            animator.SetBool("isWalking", false);

        animator.SetFloat("LookX", attackDirection.x);
        animator.SetFloat("LookY", attackDirection.y);

        animator.SetFloat("LastInputX", attackDirection.x);
        animator.SetFloat("LastInputY", attackDirection.y);

        core.playerMovement.netLastInput.Value =
            attackDirection;

        if (!isRunningLocal)
        {
            combo++;

            if (combo > comboNumber)
                combo = 1;
        }
        else
        {
            combo = 1;
        }

        comboTempo = 0f;
        comboTiming = 2f;

        if (debugAttackEvents)
        {
            Debug.Log(
                $"[Knight] Attack START " +
                $"Sequence={attackSequence}, " +
                $"Combo={currentComboCache}"
            );
        }

        PlayAttackAnimationServerRpc(
            isRunningLocal,
            isMoving,
            attackDirection,
            currentComboCache
        );
    }

    [ServerRpc]
    private void PlayAttackAnimationServerRpc(
        bool runAttack,
        bool walkAttack,
        Vector2 dir,
        int comboCount)
    {
        PlayAttackAnimationClientRpc(
            runAttack,
            walkAttack,
            dir,
            comboCount
        );
    }

    [ClientRpc]
    private void PlayAttackAnimationClientRpc(
        bool runAttack,
        bool walkAttack,
        Vector2 dir,
        int comboCount)
    {
        // Owner đã chạy animation cục bộ.
        if (IsOwner)
            return;

        if (animator == null)
            return;

        attackSequence++;
        attackActive = true;
        attackStartedAt = Time.time;
        attackStateSeen = false;
        damageProcessedThisAttack = false;

        currentComboCache = comboCount;
        attackDirection = dir;

        damagedEnemies.Clear();

        animator.SetBool("isAttacking", true);
        animator.SetBool("isRunAttacking", runAttack);
        animator.SetBool("isWalkAttacking", walkAttack);

        if (!runAttack && !walkAttack)
            animator.SetBool("isWalking", false);

        animator.SetFloat("LookX", dir.x);
        animator.SetFloat("LookY", dir.y);

        animator.SetFloat("LastInputX", dir.x);
        animator.SetFloat("LastInputY", dir.y);

        if (core != null &&
            core.playerMovement != null)
        {
            core.playerMovement.isAttacking = true;

            core.playerMovement.canMoveWhileAttacking =
                runAttack || walkAttack;
        }
    }

    // ========================================
    // ANIMATION EVENTS
    // ========================================

    public void StartAttack()
    {
        if (!IsOwner || !attackActive)
            return;

        SoundEffectManager.Play("Melee Effect", true);

        if (debugAttackEvents)
        {
            Debug.Log(
                $"[Knight] StartAttack Event " +
                $"Sequence={attackSequence}"
            );
        }
    }

    public void DealDamageEvent()
    {
        if (!IsOwner || !attackActive)
            return;

        if (damageProcessedThisAttack)
        {
            if (debugAttackEvents)
            {
                Debug.LogWarning(
                    "[Knight] Duplicate DealDamageEvent ignored."
                );
            }

            return;
        }

        if (attackPoint == null ||
            core == null ||
            core.playerStats == null)
        {
            return;
        }

        damageProcessedThisAttack = true;

        Collider2D[] hitColliders =
            Physics2D.OverlapCircleAll(
                attackPoint.position,
                attackRange,
                enemyLayer
            );

        float stanceMultiplier = 1f;

        if (isRunAttacking)
        {
            stanceMultiplier = runAttackMultiplier;
        }
        else if (!isWalkAttacking)
        {
            stanceMultiplier = 1.5f;
        }

        bool hasHitAnyEnemy = false;

        Vector3 basePosition =
            transform.parent != null
                ? transform.parent.position
                : transform.position;

        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider == null)
                continue;

            // Chỉ nhận collider EnemyHurtbox.
            if (hitCollider.gameObject.layer !=
                enemyHurtboxLayer)
            {
                continue;
            }

            Enemy enemy =
                hitCollider.GetComponentInParent<Enemy>();

            if (enemy == null ||
                enemy.IsDead ||
                enemy.netHealth.Value <= 0)
            {
                continue;
            }

            // Lọc góc trước khi đánh dấu đã trúng.
            Vector2 dirToEnemy =
                (enemy.transform.position -
                 basePosition).normalized;

            if (Vector2.Angle(
                    attackDirection,
                    dirToEnemy) > attackAngle / 2f)
            {
                continue;
            }

            // Chỉ cho phép một hit mỗi Enemy.
            if (!damagedEnemies.Add(enemy))
                continue;

            float comboScale =
                checkCombo(currentComboCache);

            if (isRunAttacking)
                comboScale = 1f;

            float rawDamage =
                core.playerStats.finalPhysicalAttack *
                comboScale *
                stanceMultiplier;

            bool isCritical =
                Random.Range(0f, 100f) <
                core.playerStats.finalCritRate;

            if (isCritical)
                rawDamage *= 2f;

            int finalDamage =
                Mathf.RoundToInt(rawDamage);

            bool forceKnockback =
                isRunAttacking && causesKnockback;

            if (IsServer)
            {
                enemy.TakeDamage(
                    finalDamage,
                    DamageSourceType.Knight,
                    transform.parent,
                    isCritical,
                    forceKnockback
                );
            }
            else
            {
                NetworkObject enemyNetObj =
                    enemy.GetComponent<NetworkObject>();

                if (enemyNetObj != null &&
                    enemyNetObj.IsSpawned)
                {
                    RequestDealDamageServerRpc(
                        enemyNetObj.NetworkObjectId,
                        finalDamage,
                        isCritical,
                        forceKnockback
                    );
                }
            }

            hasHitAnyEnemy = true;
        }

        if (hasHitAnyEnemy &&
            CinemachineShaker.Instance != null)
        {
            CinemachineShaker.Instance.TriggerShake(
                hitShakeIntensity,
                hitShakeFrequency,
                hitShakeDuration
            );
        }
    }

    // Goi tu Animation Event, van giu ten cu de khong phai sua clip.
    public void EndAttack()
    {
        FinishAttack("Animation Event");
    }

    private void FinishAttack(string reason)
    {
        if (debugAttackEvents)
        {
            Debug.Log(
                $"[Knight] FinishAttack ({reason}) " +
                $"Sequence={attackSequence}, Active={attackActive}, " +
                $"Frame={Time.frameCount}, Owner={IsOwner}", this);
        }

        if (!attackActive)
            return;

        // Canh bao khi dung fallback, de truy duoc clip/transition gay loi.
        if (reason != "Animation Event")
        {
            Debug.LogWarning(
                $"[KnightNormalAttack] Auto-recover: {reason}. " +
                $"Sequence={attackSequence}, Object={name}", this);
        }

        attackActive = false;
        attackStateSeen = false;

        ResetAttackAnimator();
        attackPressed = false;

        if (core != null && core.playerMovement != null)
        {
            core.playerMovement.isAttacking = false;
            core.playerMovement.canMoveWhileAttacking = false;
        }
    }

    private void ResetAttackAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool("isAttacking", false);
        animator.SetBool("isWalkAttacking", false);
        animator.SetBool("isRunAttacking", false);
    }

    private void CancelAttack()
    {
        if (!attackActive &&
            !isAttacking &&
            !isWalkAttacking &&
            !isRunAttacking)
        {
            return;
        }

        attackActive = false;
        attackStateSeen = false;
        attackPressed = false;
        damageProcessedThisAttack = false;

        damagedEnemies.Clear();

        ResetAttackAnimator();

        if (core != null &&
            core.playerMovement != null)
        {
            core.playerMovement.isAttacking = false;
            core.playerMovement.canMoveWhileAttacking = false;
        }
    }

    [ServerRpc]
    private void RequestDealDamageServerRpc(
        ulong enemyNetworkId,
        int finalDamage,
        bool isCritical,
        bool forceKnockback,
        ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.SpawnManager == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager
            .SpawnedObjects.TryGetValue(
                enemyNetworkId,
                out NetworkObject enemyObj))
        {
            return;
        }

        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null ||
            enemy.IsDead ||
            enemy.netHealth.Value <= 0)
        {
            return;
        }

        Transform attackerTransform = null;

        if (NetworkManager.Singleton.ConnectedClients
            .TryGetValue(
                rpcParams.Receive.SenderClientId,
                out var client))
        {
            if (client.PlayerObject != null)
                attackerTransform = client.PlayerObject.transform;
        }

        enemy.TakeDamage(
            finalDamage,
            DamageSourceType.Knight,
            attackerTransform,
            isCritical,
            forceKnockback
        );
    }

    // ========================================
    // COMBO DAMAGE
    // ========================================

    private float checkCombo(int c)
    {
        switch (c)
        {
            case 1:
                return 1f;

            case 2:
                return 1.2f;

            case 3:
                return 1.5f;

            default:
                return 1f;
        }
    }

    // ========================================
    // AIM DIRECTION
    // ========================================

    private void UpdateAttackPointDirection()
    {
        if (Camera.main == null ||
            isAttacking ||
            attackActive)
        {
            return;
        }

        if (attackPoint == null ||
            core == null ||
            core.playerMovement == null)
        {
            return;
        }

        Vector3 basePosition =
            transform.parent != null
                ? transform.parent.position
                : transform.position;

        Vector2 fallbackDir =
            core.playerMovement.moveInput.magnitude > 0.01f
                ? core.playerMovement.moveInput.normalized
                : attackDirection;

        if (targetSelector != null)
        {
            attackDirection =
                targetSelector.GetAimDirection(
                    basePosition,
                    fallbackDir
                );
        }
        else
        {
            attackDirection = fallbackDir;
        }

        if (attackDirection.sqrMagnitude < 0.0001f)
            attackDirection = Vector2.down;

        attackPoint.position =
            basePosition +
            (Vector3)attackDirection * attackRange;
    }

    // ========================================
    // DEBUG GIZMOS
    // ========================================

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Vector3 basePosition =
            transform.parent != null
                ? transform.parent.position
                : transform.position;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            attackRange
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            basePosition,
            attackPoint.position
        );

        if (attackDirection == Vector2.zero)
            return;

        Gizmos.color = Color.cyan;

        Vector3 rightLimit =
            Quaternion.Euler(
                0,
                0,
                attackAngle / 2f
            ) * attackDirection;

        Vector3 leftLimit =
            Quaternion.Euler(
                0,
                0,
                -attackAngle / 2f
            ) * attackDirection;

        float reach =
            Vector3.Distance(
                basePosition,
                attackPoint.position
            ) + attackRange;

        Gizmos.DrawLine(
            basePosition,
            basePosition + rightLimit * reach
        );

        Gizmos.DrawLine(
            basePosition,
            basePosition + leftLimit * reach
        );
    }
}

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MageNormalAttack : NetworkBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;

    [Header("Attack Settings")]
    public int attackManaCost = 5;
    public float mageDamageMultiplier = 1.5f;

    [SerializeField] private PlayerCore core;

    public bool isAttacking => animator.GetBool("isAttacking");

    private bool attackPressed = false;
    private Vector2 attackDirection;
    private bool hasFiredThisAttack = false;

    [SerializeField] private CombatTargetSelector targetSelector;

    private Queue<GameObject> projectilePool = new Queue<GameObject>();
    public int poolSize = 10;

    private void Start()
    {
        if (IsServer)
        {
            InitializePool();
        }
    }

    private void InitializePool()
    {
        GameObject container = new GameObject("ProjectilePool_Container");

        for (int i = 0; i < poolSize; i++)
        {
            GameObject proj = Instantiate(projectilePrefab, container.transform);
            NetworkObject netObj = proj.GetComponent<NetworkObject>();
            netObj.Spawn();

            proj.SetActive(false);
            projectilePool.Enqueue(proj);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateAimDirection();

        if (PauseController.IsGamePause)
        {
            animator.SetBool("isAttacking", false);
            if (core.playerMovement != null) core.playerMovement.isAttacking = false;
            return;
        }

        if (attackPressed)
        {
            attackPressed = false;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TryAttack();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (PauseController.IsGamePause || !context.performed) return;
        if (core.playerMovement != null && core.playerMovement.IsDead) return;

        attackPressed = true;
    }

    private void TryAttack()
    {
        if (isAttacking) return;
        if (core.playerMovement != null && core.playerMovement.IsDead) return;
        if (!core.playerStats.CanAttack || !GameStateManager.CanProcessInput()) return;

        if (core.playerStats != null && core.playerVitals.mageMP >= attackManaCost)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        core.playerVitals.UseMP(attackManaCost, false);
        hasFiredThisAttack = false;

        animator.SetBool("isAttacking", true);
        animator.SetBool("isWalking", false);

        if (core.playerMovement != null)
        {
            core.playerMovement.isAttacking = true;
            core.playerMovement.canMoveWhileAttacking = false;
            core.playerMovement.netLastInput.Value = attackDirection;

            if (core.playerMovement.rb != null)
            {
                core.playerMovement.rb.linearVelocity = Vector2.zero;
            }
        }

        animator.SetFloat("LookX", attackDirection.x);
        animator.SetFloat("LookY", attackDirection.y);
        animator.SetFloat("LastInputX", attackDirection.x);
        animator.SetFloat("LastInputY", attackDirection.y);

        if (IsOwner)
        {
            PlayAttackAnimationServerRpc(attackDirection);
        }
    }

    private void StartAttack()
    {
        FireProjectileEvent();
    }

    [ServerRpc]
    private void PlayAttackAnimationServerRpc(Vector2 dir)
    {
        PlayAttackAnimationClientRpc(dir);
    }

    [ClientRpc]
    private void PlayAttackAnimationClientRpc(Vector2 dir)
    {
        if (IsOwner) return;

        animator.SetBool("isAttacking", true);
        animator.SetBool("isWalking", false);

        animator.SetFloat("LookX", dir.x);
        animator.SetFloat("LookY", dir.y);
        animator.SetFloat("LastInputX", dir.x);
        animator.SetFloat("LastInputY", dir.y);
    }

    private void UpdateAimDirection()
    {
        if (isAttacking) return;

        Vector3 basePosition = transform.parent != null ? transform.parent.position : transform.position;

        Vector2 fallbackDir = core != null && core.playerMovement != null &&
                              core.playerMovement.moveInput.sqrMagnitude > 0.0001f
            ? core.playerMovement.moveInput
            : (attackDirection.sqrMagnitude > 0.0001f ? attackDirection : Vector2.down);

        Vector2 requestedDirection = targetSelector != null && Camera.main != null
            ? targetSelector.GetAimDirection(basePosition, fallbackDir)
            : fallbackDir;

        attackDirection = SnapToFourDirections(requestedDirection);
    }

    // Mage's base attack and projectile are restricted to up/down/left/right.
    // The dominant axis determines the facing direction; diagonal aiming is discarded.
    private static Vector2 SnapToFourDirections(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return Vector2.down;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? Vector2.right : Vector2.left;

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }

    public void FireProjectileEvent()
    {
        if (!IsSpawned || !IsOwner) return;

        if (hasFiredThisAttack) return;
        hasFiredThisAttack = true;

        SoundEffectManager.Play("Magic Shoot", true);

        float rawDamage = core.playerStats.finalMagicAttack * mageDamageMultiplier;
        bool isCritical = false;

        if (UnityEngine.Random.Range(0f, 100f) < core.playerStats.finalCritRate)
        {
            isCritical = true;
            rawDamage *= 2;
        }

        int finalDamage = Mathf.RoundToInt(rawDamage);

        RequestSpawnProjectileServerRpc(attackDirection, finalDamage, isCritical);
    }

    [ServerRpc]
    private void RequestSpawnProjectileServerRpc(Vector2 dir, int damage, bool isCrit, ServerRpcParams rpcParams = default)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject proj = GetProjectileFromPool();
        if (proj != null)
        {
            proj.transform.position = firePoint.position;

            ManaProjectile manaProj = proj.GetComponent<ManaProjectile>();
            if (manaProj != null)
            {
                manaProj.InitializeAndActivate(dir, projectileSpeed, damage, isCrit, rpcParams.Receive.SenderClientId);
            }
        }
    }

    private GameObject GetProjectileFromPool()
    {
        if (projectilePool.Count == 0) return null;

        GameObject proj = projectilePool.Dequeue();
        projectilePool.Enqueue(proj);

        return proj;
    }

    public void EndAttack()
    {
        animator.SetBool("isAttacking", false);
        attackPressed = false;

        if (core.playerMovement != null)
        {
            core.playerMovement.isAttacking = false;
            core.playerMovement.canMoveWhileAttacking = false;
        }
    }
}
using System;
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

        animator.SetTrigger("Attack");

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

        animator.SetTrigger("Attack");
    }

    private void UpdateAimDirection()
    {
        if (Camera.main == null || isAttacking) return;

        Vector3 basePosition = transform.parent != null ? transform.parent.position : transform.position;

        Vector2 fallbackDir = core.playerMovement != null && core.playerMovement.moveInput.magnitude > 0.01f
            ? core.playerMovement.moveInput.normalized
            : attackDirection;

        if (targetSelector != null)
        {
            attackDirection = targetSelector.GetAimDirection(basePosition, fallbackDir);
        }
        else
        {
            attackDirection = fallbackDir;
        }
    }

    public void FireProjectileEvent()
    {
        if (!IsOwner) return;

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

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        NetworkObject netObj = proj.GetComponent<NetworkObject>();
        netObj.Spawn();

        ManaProjectile manaProj = proj.GetComponent<ManaProjectile>();
        if (manaProj != null)
        {
            manaProj.Initialize(dir, projectileSpeed, damage, isCrit, rpcParams.Receive.SenderClientId);
        }
    }

    public void EndAttack()
    {
        animator.SetBool("isAttacking", false);
        animator.ResetTrigger("Attack");
        attackPressed = false;

        if (core.playerMovement != null)
        {
            core.playerMovement.isAttacking = false;
            core.playerMovement.canMoveWhileAttacking = false;
        }
    }
}
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

    private PlayerStats playerStats => GetComponentInParent<PlayerStats>();
    private PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

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
            if (playerMovement != null) playerMovement.isAttacking = false;
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
        if (playerMovement != null && playerMovement.IsDead) return;

        attackPressed = true;
    }

    private void TryAttack()
    {
        if (isAttacking) return;
        if (playerMovement != null && playerMovement.IsDead) return;
        if (!PlayerStats.Instance.CanAttack || !GameStateManager.CanProcessInput()) return;

        if (playerStats != null && playerStats.mageMP >= attackManaCost)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        playerStats.UseMP(attackManaCost, false);

        hasFiredThisAttack = false;

        animator.SetBool("isAttacking", true);
        animator.SetBool("isWalking", false);

        if (playerMovement != null)
        {
            playerMovement.isAttacking = true;
            playerMovement.canMoveWhileAttacking = false;
            playerMovement.netLastInput.Value = attackDirection;

            if (playerMovement.rb != null)
            {
                playerMovement.rb.linearVelocity = Vector2.zero;
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

        Vector2 fallbackDir = playerMovement != null && playerMovement.moveInput.magnitude > 0.01f
            ? playerMovement.moveInput.normalized
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

        float rawDamage = playerStats.finalMagicAttack * mageDamageMultiplier;
        bool isCritical = false;

        if (UnityEngine.Random.Range(0f, 100f) < playerStats.finalCritRate)
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

        if (playerMovement != null)
        {
            playerMovement.isAttacking = false;
            playerMovement.canMoveWhileAttacking = false;
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStats playerStats;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
    }

    private void Start()
    {
        if (playerStats == null) return;

        playerStats.netIsDead.OnValueChanged += OnDeathStateChanged;

        if (playerStats.netIsDead.Value && !DeathService.IsRespawningFlag)
        {
            ApplyDeathVisuals();
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.netIsDead.OnValueChanged -= OnDeathStateChanged;
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
    }

    private void ResetVisuals()
    {
        animator.ResetTrigger("Die");
        animator.Play("Idle");
    }

    public void OnDeathAnimationFinished()
    {
        if (playerStats != null && playerStats.IsOwner && playerStats.netIsDead.Value && !DeathService.IsRespawningFlag)
        {
            GameOverUIAdapter.Instance.ShowGameOverUI();
        }
    }
}
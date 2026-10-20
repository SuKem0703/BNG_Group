
using Unity.Netcode;
using UnityEngine;

public class ClassVisualHandler : MonoBehaviour
{
    public enum ClassType { Knight, Mage, Archer }

    [Header("Phân loại Class")]
    [SerializeField] private ClassType classType;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Controllers theo từng Tier (Index 0 = Tier 0, Index 1 = Tier 1...)")]
    [SerializeField] private RuntimeAnimatorController[] tierAnimators;

    private PlayerStats playerStats;

    private void Start()
    {
        // Lấy PlayerStats từ GameObject cha (Root)
        playerStats = GetComponentInParent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogWarning(
                $"[{nameof(ClassVisualHandler)}] Không tìm thấy PlayerStats ở GameObject cha.",
                this
            );
            return;
        }

        // Đăng ký lắng nghe sự thay đổi Tier tương ứng với từng class
        switch (classType)
        {
            case ClassType.Knight:
                playerStats.netKnightBreakthroughTier.OnValueChanged += OnTierChanged;
                UpdateAnimator(playerStats.netKnightBreakthroughTier.Value);
                break;

            case ClassType.Mage:
                playerStats.netMageBreakthroughTier.OnValueChanged += OnTierChanged;
                UpdateAnimator(playerStats.netMageBreakthroughTier.Value);
                break;

            case ClassType.Archer:
                playerStats.netArcherBreakthroughTier.OnValueChanged += OnTierChanged;
                UpdateAnimator(playerStats.netArcherBreakthroughTier.Value);
                break;
        }
    }

    private void OnDestroy()
    {
        if (playerStats == null)
            return;

        // Hủy đăng ký sự kiện để tránh rò rỉ callback
        switch (classType)
        {
            case ClassType.Knight:
                playerStats.netKnightBreakthroughTier.OnValueChanged -= OnTierChanged;
                break;

            case ClassType.Mage:
                playerStats.netMageBreakthroughTier.OnValueChanged -= OnTierChanged;
                break;

            case ClassType.Archer:
                playerStats.netArcherBreakthroughTier.OnValueChanged -= OnTierChanged;
                break;
        }
    }

    private void OnTierChanged(int previousValue, int newValue)
    {
        UpdateAnimator(newValue);
    }

    private void UpdateAnimator(int tier)
    {
        if (animator == null || tierAnimators == null)
            return;

        if (tier < 0 || tier >= tierAnimators.Length)
        {
            Debug.LogWarning(
                $"Tier {tier} nằm ngoài phạm vi tierAnimators.",
                this
            );
            return;
        }

        if (tierAnimators[tier] != null)
        {
            // Thay thế Animator Controller theo Tier đột phá
            animator.runtimeAnimatorController = tierAnimators[tier];
        }
    }
}


using System.Collections;
using UnityEngine;

public class ClassVisualHandler : MonoBehaviour
{
    public enum ClassType
    {
        Knight = 0,
        Mage = 1,
        Archer = 2
    }

    [Header("Class Settings")]
    [SerializeField] private ClassType classType;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Controllers By Tier")]
    [SerializeField]
    private RuntimeAnimatorController[] tierAnimators;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private PlayerStats playerStats;
    private Coroutine initializationCoroutine;

    private bool isSubscribed = false;
    private int currentAppliedTier = -1;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        playerStats = GetComponentInParent<PlayerStats>(true);
    }

    private void OnEnable()
    {
        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>(true);

        if (playerStats == null)
        {
            Debug.LogWarning(
                "[ClassVisualHandler] Không tìm thấy PlayerStats!",
                this
            );
            return;
        }

        if (playerStats.IsSpawned)
        {
            InitializeVisuals();
        }
        else
        {
            initializationCoroutine =
                StartCoroutine(WaitForNetworkSpawn());
        }
    }

    private IEnumerator WaitForNetworkSpawn()
    {
        while (playerStats != null &&
               !playerStats.IsSpawned)
        {
            yield return null;
        }

        initializationCoroutine = null;

        if (playerStats == null || !isActiveAndEnabled)
            yield break;

        InitializeVisuals();
    }

    private void InitializeVisuals()
    {
        UnsubscribeEvents();

        switch (classType)
        {
            case ClassType.Knight:
                playerStats.netKnightBreakthroughTier
                    .OnValueChanged += OnTierChanged;
                break;

            case ClassType.Mage:
                playerStats.netMageBreakthroughTier
                    .OnValueChanged += OnTierChanged;
                break;

            case ClassType.Archer:
                playerStats.netArcherBreakthroughTier
                    .OnValueChanged += OnTierChanged;
                break;
        }

        isSubscribed = true;

        // Luôn áp dụng giá trị đã đồng bộ.
        // Cần thiết cho client tham gia sau.
        ApplyAnimator(GetCurrentTier());
    }

    private int GetCurrentTier()
    {
        if (playerStats == null)
            return 0;

        switch (classType)
        {
            case ClassType.Knight:
                return playerStats.netKnightBreakthroughTier.Value;

            case ClassType.Mage:
                return playerStats.netMageBreakthroughTier.Value;

            case ClassType.Archer:
                return playerStats.netArcherBreakthroughTier.Value;

            default:
                return 0;
        }
    }

    private void OnTierChanged(int previousValue, int newValue)
    {
        ApplyAnimator(newValue);
    }

    private void ApplyAnimator(int tier)
    {
        if (animator == null ||
            tierAnimators == null ||
            tierAnimators.Length == 0)
        {
            return;
        }

        if (tier < 0 || tier >= tierAnimators.Length)
        {
            Debug.LogWarning(
                $"[ClassVisualHandler] {classType} " +
                $"Tier {tier} không tồn tại trong danh sách!",
                this
            );
            return;
        }

        RuntimeAnimatorController targetController =
            tierAnimators[tier];

        if (targetController == null)
        {
            Debug.LogWarning(
                $"[ClassVisualHandler] " +
                $"{classType} Tier {tier} chưa có Animator Controller!",
                this
            );
            return;
        }

        // Không gán lại khi Controller không thay đổi.
        if (animator.runtimeAnimatorController != targetController)
        {
            animator.runtimeAnimatorController =
                targetController;
        }

        currentAppliedTier = tier;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[ClassVisualHandler] " +
                $"Class={classType}, " +
                $"Tier={currentAppliedTier}, " +
                $"Controller={targetController.name}, " +
                $"Player={playerStats.name}"
            );
        }
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed || playerStats == null)
            return;

        switch (classType)
        {
            case ClassType.Knight:
                playerStats.netKnightBreakthroughTier
                    .OnValueChanged -= OnTierChanged;
                break;

            case ClassType.Mage:
                playerStats.netMageBreakthroughTier
                    .OnValueChanged -= OnTierChanged;
                break;

            case ClassType.Archer:
                playerStats.netArcherBreakthroughTier
                    .OnValueChanged -= OnTierChanged;
                break;
        }

        isSubscribed = false;
    }

    private void OnDisable()
    {
        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
            initializationCoroutine = null;
        }

        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}

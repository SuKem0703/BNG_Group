
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVFXController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCore core;
    [SerializeField] private Animator knightAnimator;
    [SerializeField] private Animator mageAnimator;

    [Header("Casting State")]
    [SerializeField] private bool isCasting;

    public bool IsCasting => isCasting;

    [Header("Healing Aura")]
    [SerializeField] private Transform healingBuffAnchor;
    [SerializeField] private GameObject healingBuffVFXPrefab;

    private GameObject activeHealingBuffVFX;

    // Kỹ năng đang thi triển.
    private string pendingSkillID;
    private int pendingSkillRank;
    private PlayerClassType pendingClass;

    private static readonly int IsCastingHash =
        Animator.StringToHash("isCasting");

    // Cooldown được lưu tại Player, không lưu trong Hotbar UI.
    private struct SkillCooldownState
    {
        public double endTime;
        public float duration;

        public SkillCooldownState(float duration)
        {
            this.duration = duration;
            endTime = Time.timeAsDouble + duration;
        }
    }

    private readonly Dictionary<
        (PlayerClassType, string),
        SkillCooldownState
    > skillCooldowns = new();

    private void Awake()
    {
        if (core == null)
            core = GetComponentInParent<PlayerCore>();
    }

    private Animator GetCurrentAnimator()
    {
        if (core == null || core.classController == null)
            return null;

        return core.classController.IsKnightActive
            ? knightAnimator
            : mageAnimator;
    }

    // ==================================================
    // COOLDOWN
    // ==================================================

    public float GetCooldownRemaining(
        string skillID,
        PlayerClassType playerClass)
    {
        var key = (playerClass, skillID);

        if (!skillCooldowns.TryGetValue(key, out var cooldown))
            return 0f;

        float remaining = Mathf.Max(
            0f,
            (float)(cooldown.endTime - Time.timeAsDouble)
        );

        if (remaining <= 0f)
            skillCooldowns.Remove(key);

        return remaining;
    }

    public bool IsSkillOnCooldown(
        string skillID,
        PlayerClassType playerClass)
    {
        return GetCooldownRemaining(skillID, playerClass) > 0f;
    }

    // Giá trị dùng cho Image.fillAmount:
    // 1 = vừa bắt đầu cooldown
    // 0 = cooldown đã kết thúc
    public float GetCooldownFill(string skillID)
    {
        if (core == null ||
            core.classController == null ||
            string.IsNullOrEmpty(skillID))
        {
            return 0f;
        }

        PlayerClassType playerClass =
            core.classController.GetCurrentClassType();

        var key = (playerClass, skillID);

        if (!skillCooldowns.TryGetValue(key, out var cooldown))
            return 0f;

        float remaining = GetCooldownRemaining(
            skillID,
            playerClass
        );

        if (remaining <= 0f || cooldown.duration <= 0f)
            return 0f;

        return Mathf.Clamp01(
            remaining / cooldown.duration
        );
    }

    private void StartSkillCooldown(
        string skillID,
        PlayerClassType playerClass,
        float duration)
    {
        duration = Mathf.Max(0f, duration);

        var key = (playerClass, skillID);

        if (duration <= 0f)
        {
            skillCooldowns.Remove(key);
            return;
        }

        skillCooldowns[key] =
            new SkillCooldownState(duration);
    }

    // ==================================================
    // CASTING
    // ==================================================

    public bool TryStartCasting()
    {
        if (core == null ||
            !core.IsSpawned ||
            !core.IsOwner ||
            core.classController == null)
        {
            return false;
        }

        if (isCasting || PauseController.IsGamePause)
            return false;

        if (!SaveController.IsDataLoaded ||
            !GameStateManager.CanProcessInput())
        {
            return false;
        }

        if (core.playerMovement != null)
        {
            if (core.playerMovement.IsDead ||
                core.playerMovement.isAttacking ||
                core.playerMovement.isDashing)
            {
                return false;
            }
        }

        if (core.playerVitals != null)
        {
            if (core.playerVitals.isProcessingDeath ||
                core.playerVitals.isGameOver ||
                core.playerVitals.netIsDead.Value)
            {
                return false;
            }
        }

        if (GetCurrentAnimator() == null)
            return false;

        StartCastingVisual();
        core.SyncCastingServerRpc(true);

        return true;
    }

    // Được Hotbar sử dụng.
    public bool TryStartCasting(SkillData skill)
    {
        if (skill == null ||
            skill.skillType != SkillType.Active ||
            core == null ||
            core.classController == null ||
            core.playerVitals == null ||
            SkillTreeService.Instance == null)
        {
            return false;
        }

        bool isSupported =
    skill.effectID == EffectService.HealRegenID ||
    skill.effectID == EffectService.CloverBuffID ||
    skill.effectID == EffectService.FeatherBuffID;

        if (!isSupported)
        {
            Debug.LogWarning(
                $"[Skill] Chưa hỗ trợ effectID: {skill.effectID}"
            );
            return false;
        }

        int rank =
            SkillTreeService.Instance.GetSkillRank(skill.skillID);

        if (rank <= 0)
            return false;

        PlayerClassType playerClass =
            core.classController.GetCurrentClassType();

        if (playerClass != PlayerClassType.Knight &&
            playerClass != PlayerClassType.Mage)
        {
            return false;
        }

        if (!skill.CanBeUsedBy(playerClass))
            return false;

        // Kiểm tra cooldown trước khi cast.
        if (IsSkillOnCooldown(skill.skillID, playerClass))
            return false;

        bool isKnight = playerClass == PlayerClassType.Knight;

        int manaCost = Mathf.CeilToInt(
            Mathf.Max(0f, skill.GetManaCost(rank))
        );

        int currentMP = core.playerVitals.GetMP(isKnight);

        // Chưa đủ MP thì không kích hoạt animation.
        if (currentMP < manaCost)
        {
            GameNotify.Show(
                $"Không đủ MP! ({currentMP}/{manaCost})"
            );
            return false;
        }

        if (!TryStartCasting())
            return false;

        // Ghi nhớ kỹ năng và class đang cast.
        pendingSkillID = skill.skillID;
        pendingSkillRank = rank;
        pendingClass = playerClass;

        return true;
    }

    public void StartCastingVisual()
    {
        if (isCasting)
            return;

        Animator animator = GetCurrentAnimator();

        if (animator == null)
            return;

        isCasting = true;

        animator.SetBool(IsCastingHash, true);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);

        if (core != null &&
            core.playerMovement != null &&
            core.playerMovement.IsOwner &&
            core.playerMovement.rb != null)
        {
            core.playerMovement.rb.linearVelocity = Vector2.zero;
        }
    }

    public void EndCasting()
    {
        if (!isCasting)
            return;

        string completedSkillID = pendingSkillID;
        int completedRank = pendingSkillRank;
        PlayerClassType completedClass = pendingClass;

        StopCastingVisual();

        if (core == null || !core.IsSpawned || !core.IsOwner)
            return;

        core.SyncCastingServerRpc(false);

        if (string.IsNullOrEmpty(completedSkillID) ||
            completedRank <= 0)
        {
            return;
        }

        if (core.playerVitals == null ||
            core.classController == null ||
            core.playerMovement == null ||
            core.playerMovement.IsDead ||
            core.playerVitals.isProcessingDeath ||
            core.playerVitals.isGameOver ||
            core.playerVitals.netIsDead.Value)
        {
            return;
        }

        if (core.classController.GetCurrentClassType() != completedClass)
            return;

        SkillData skill = SkillDictionary.Instance != null
            ? SkillDictionary.Instance.GetSkill(completedSkillID)
            : null;

        if (skill == null || skill.skillType != SkillType.Active)
        {
            return;
        }

        bool isSupported =
    skill.effectID == EffectService.HealRegenID ||
    skill.effectID == EffectService.CloverBuffID ||
    skill.effectID == EffectService.FeatherBuffID;

        if (!isSupported)
            return;

        if (!skill.CanBeUsedBy(completedClass))
            return;

        if (skill.effectID == EffectService.CloverBuffID && completedClass != PlayerClassType.Knight)
        {
            return;
        }

        if (SkillTreeService.Instance == null || SkillTreeService.Instance.GetSkillRank(completedSkillID) < completedRank)
        {
            return;
        }

        // Kiểm tra lại cooldown.
        if (IsSkillOnCooldown(completedSkillID, completedClass))
            return;

        bool isKnight =
            completedClass == PlayerClassType.Knight;

        int manaCost = Mathf.CeilToInt(
            Mathf.Max(0f, skill.GetManaCost(completedRank))
        );

        if (!core.playerVitals.TryUseSkillMP(manaCost, isKnight))
        {
            GameNotify.Show("Không đủ MP để hoàn thành kỹ năng!");
            return;
        }

        StartSkillCooldown(
            completedSkillID,
            completedClass,
            skill.GetCooldown(completedRank)
        );

        core.RequestBuff(completedSkillID, completedRank);

        Debug.Log(
            $"[Skill] {completedSkillID} | " +
            $"Rank: {completedRank} | " +
            $"MP: -{manaCost} | " +
            $"Cooldown: {skill.GetCooldown(completedRank):F1}s"
        );
    }

    // Dùng khi chết hoặc bị hủy Casting.
    public void CancelCasting()
    {
        bool wasCasting = isCasting;

        StopCastingVisual();

        if (wasCasting &&
            core != null &&
            core.IsSpawned &&
            core.IsOwner)
        {
            core.SyncCastingServerRpc(false);
        }
    }

    public void StopCastingVisual()
    {
        isCasting = false;

        pendingSkillID = null;
        pendingSkillRank = 0;

        Animator animator = GetCurrentAnimator();

        if (animator != null)
            animator.SetBool(IsCastingHash, false);
    }

    // ==================================================
    // HEALING VFX
    // ==================================================

    public void ShowHealingBuffVFX(float duration)
    {
        if (activeHealingBuffVFX != null)
        {
            Destroy(activeHealingBuffVFX);
            activeHealingBuffVFX = null;
        }

        if (healingBuffVFXPrefab == null)
        {
            Debug.LogWarning(
                "[HealingBuff] Chưa gán HealingBuffVFX Prefab!",
                this
            );
            return;
        }

        Transform anchor = healingBuffAnchor != null
            ? healingBuffAnchor
            : transform;

        activeHealingBuffVFX = Instantiate(
            healingBuffVFXPrefab,
            anchor
        );

        activeHealingBuffVFX.transform.localPosition =
            Vector3.zero;

        activeHealingBuffVFX.transform.localRotation =
            Quaternion.identity;

        LoopVFX effect =
            activeHealingBuffVFX.GetComponent<LoopVFX>();

        if (effect != null)
        {
            effect.Initialize(core, duration);
        }
        else
        {
            Debug.LogWarning(
                "[HealingBuff] Prefab thiếu HealingBuffVFX!",
                activeHealingBuffVFX
            );

            Destroy(activeHealingBuffVFX, duration);
        }
    }

    [ContextMenu("DEBUG - Start Casting")]
    private void DebugStartCasting()
    {
        StopCastingVisual();
        StartCastingVisual();

        Animator animator = GetCurrentAnimator();

        Debug.Log(
            $"[Casting Debug] Animator: {animator?.name}, " +
            $"VFX IsCasting: {isCasting}, " +
            $"Animator IsCasting: " +
            $"{animator?.GetBool(IsCastingHash)}"
        );
    }


    [Serializable]
    public class BuffActivationVFXEntry
    {
        public string effectID;
        public GameObject prefab;
    }

    [Header("Buff Activation VFX")]
    [SerializeField] private Transform buffActivationAnchor;

    [SerializeField]
    private List<BuffActivationVFXEntry> buffActivationVFX =
        new List<BuffActivationVFXEntry>();

    public void PlayBuffActivationVFX(string effectID)
    {
        if (string.IsNullOrEmpty(effectID))
            return;

        BuffActivationVFXEntry entry =
            buffActivationVFX.Find(
                x => x != null && x.effectID == effectID
            );

        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning(
                $"[PlayerVFX] Chưa đăng ký VFX: {effectID}"
            );
            return;
        }

        Transform parent =
            buffActivationAnchor != null
                ? buffActivationAnchor
                : transform;

        GameObject instance = Instantiate(
            entry.prefab,
            parent
        );

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        OneShotVFX vfx =
            instance.GetComponent<OneShotVFX>();

        if (vfx != null)
        {
            vfx.Initialize();
        }
        else
        {
            Debug.LogWarning(
                $"[PlayerVFX] Prefab {entry.prefab.name} " +
                "thiếu OneShotVFX!"
            );

            Destroy(instance, 1f);
        }
    }

}

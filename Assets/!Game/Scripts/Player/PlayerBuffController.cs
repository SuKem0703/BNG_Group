
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerCore))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerBuffController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCore core;

    private void Awake()
    {
        if (core == null)
            core = GetComponent<PlayerCore>();
    }

    public void RequestBuff(string skillID, int rank)
    {
        if (!IsSpawned ||
            !IsOwner ||
            string.IsNullOrEmpty(skillID) ||
            rank <= 0)
        {
            return;
        }

        RequestBuffServerRpc(skillID, rank);
    }

    [ServerRpc]
    private void RequestBuffServerRpc(
        string skillID,
        int rank)
    {
        if (core == null ||
            core.playerVitals == null ||
            core.playerVitals.netIsDead.Value)
        {
            return;
        }

        SkillData skill = SkillDictionary.Instance != null
            ? SkillDictionary.Instance.GetSkill(skillID)
            : null;

        if (skill == null ||
            skill.skillType != SkillType.Active ||
            rank < 1 ||
            rank > skill.maxRank)
        {
            return;
        }

        switch (skill.effectID)
        {
            case EffectService.HealRegenID:
                ApplyHealingBuff(skill, rank);
                break;

            case EffectService.CloverBuffID:
            case EffectService.FeatherBuffID:
                ApplySelfBuff(skill, rank);
                break;

            default:
                Debug.LogWarning(
                    $"[PlayerBuff] Unsupported effect: {skill.effectID}"
                );
                break;
        }
    }

    // ==========================================
    // HEALING - PARTY BUFF
    // ==========================================

    private void ApplyHealingBuff(SkillData skill, int rank)
    {
        float duration = skill.GetEffectDuration(rank);

        float healFractionPerTick =
            Mathf.Clamp01(skill.GetSkillValue(rank));

        if (duration <= 0f || healFractionPerTick <= 0f)
            return;

        if (NetworkManager == null)
            return;

        foreach (NetworkClient client in
                 NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            PlayerCore target = client.PlayerObject
                .GetComponentInChildren<PlayerCore>(true);

            if (target == null ||
                !target.IsSpawned ||
                target.gameObject.scene != gameObject.scene ||
                target.playerVitals == null ||
                target.playerVitals.netIsDead.Value)
            {
                continue;
            }

            PlayerBuffController targetBuff =
                target.GetComponent<PlayerBuffController>();

            if (targetBuff == null || !targetBuff.IsSpawned)
                continue;

            targetBuff.ApplyBuffClientRpc(
                EffectService.HealRegenID,
                duration,
                healFractionPerTick,
                0f
            );
        }
    }

    // ==========================================
    // CLOVER / FEATHER - SELF BUFF
    // ==========================================

    private void ApplySelfBuff(SkillData skill, int rank)
    {
        if (core.classController == null)
            return;

        // Giữ nguyên giới hạn Clover cho Knight.
        if (skill.effectID == EffectService.CloverBuffID &&
            !core.classController.IsKnightActive)
        {
            return;
        }

        float duration = skill.GetEffectDuration(rank);

        float primaryValue =
            Mathf.Clamp01(skill.GetSkillValue(rank));

        float secondaryValue =
            Mathf.Clamp01(skill.GetSecondarySkillValue(rank));

        if (duration <= 0f ||
            (primaryValue <= 0f && secondaryValue <= 0f))
        {
            return;
        }

        ApplyBuffClientRpc(
            skill.effectID,
            duration,
            primaryValue,
            secondaryValue
        );
    }

    // ==========================================
    // APPLY EFFECT ON CLIENTS
    // ==========================================

    [ClientRpc]
    private void ApplyBuffClientRpc(
        string effectID,
        float duration,
        float value,
        float secondaryValue)
    {
        if (IsOwner)
        {
            if (EffectService.Instance == null)
            {
                Debug.LogError(
                    $"[PlayerBuff] Missing EffectService: {effectID}"
                );
                return;
            }

            EffectService.Instance.AddEffect(
                gameObject,
                effectID,
                duration,
                value,
                secondaryValue
            );
        }

        if (core == null || core.playerVFX == null)
            return;

        switch (effectID)
        {
            case EffectService.HealRegenID:
                core.playerVFX.ShowHealingBuffVFX(duration);
                break;

            case EffectService.CloverBuffID:
            case EffectService.FeatherBuffID:
                core.playerVFX.PlayBuffActivationVFX(effectID);
                break;
        }
    }
}

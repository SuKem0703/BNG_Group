using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectService : MonoBehaviour
{
    public const string HealInstantID = "HEAL_INSTANT";
    public const string ManaInstantID = "MANA_INSTANT";
    public const string BuffSTRID = "BUFF_STR";
    public const string BuffDEXID = "BUFF_DEX";
    public const string DebuffDEXID = "DEBUFF_DEX";
    public const string HealRegenID = "HEAL_REGEN";
    public const string BurnFireID = "BURN_FIRE";
    public const string CloverBuffID = "BUFF_CLOVER";
    public const string FeatherBuffID = "BUFF_FEATHER";
    public static EffectService Instance { get; private set; }

    public class EffectData
    {
        public string effectID;
        public float duration;
        public float value;
        public float secondaryValue;
        public float remainingTime;

        internal float tickElapsed;

        public EffectData(
            string id,
            float dur,
            float val,
            float secondaryVal = 0f)
        {
            effectID = id;
            duration = dur;
            remainingTime = dur;
            value = val;
            secondaryValue = secondaryVal;
        }
    }

    private const float TickInterval = 1f;
    private readonly Dictionary<GameObject, Dictionary<string, EffectData>> activeEffects = new();

    public event Action<GameObject, EffectData> OnEffectAdded;
    public event Action<GameObject, string> OnEffectRemoved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public IReadOnlyCollection<EffectData> GetEffects(GameObject target)
    {
        if (target != null && activeEffects.TryGetValue(target, out var effects))
            return new List<EffectData>(effects.Values);
        return Array.Empty<EffectData>();
    }


    public void AddEffect(
        GameObject target,
        string effectID,
        float duration,
        float value,
        float secondaryValue = 0f)
    {
        if (target == null || string.IsNullOrWhiteSpace(effectID))
            return;

        RemoveEffect(target, effectID);

        var data = new EffectData(
            effectID,
            Mathf.Max(0f, duration),
            value,
            secondaryValue
        );

        ApplyOnStart(target, data);

        if (data.duration <= 0f)
            return;

        if (!activeEffects.TryGetValue(target, out var effects))
        {
            effects = new Dictionary<string, EffectData>();
            activeEffects.Add(target, effects);
        }

        effects.Add(effectID, data);
        OnEffectAdded?.Invoke(target, data);
    }

    public void RemoveEffect(GameObject target, string effectID)
    {
        if (target == null ||
            !activeEffects.TryGetValue(target, out var effects) ||
            !effects.TryGetValue(effectID, out var data))
            return;

        UndoOnEnd(target, data);
        effects.Remove(effectID);
        if (effects.Count == 0) activeEffects.Remove(target);
        OnEffectRemoved?.Invoke(target, effectID);
    }

    private void Update()
    {
        // Snapshots prevent collection modification while events fire.
        var targets = new List<GameObject>(activeEffects.Keys);
        foreach (GameObject target in targets)
        {
            if (target == null)
            {
                activeEffects.Remove(target);
                continue;
            }

            if (!activeEffects.TryGetValue(target, out var effects)) continue;
            var snapshot = new List<EffectData>(effects.Values);

            foreach (var data in snapshot)
            {
                if (!effects.TryGetValue(data.effectID, out var current) || current != data)
                    continue;

                float dt = Mathf.Min(Time.deltaTime, data.remainingTime);
                data.remainingTime = Mathf.Max(0f, data.remainingTime - dt);
                data.tickElapsed += dt;

                if (data.effectID == HealRegenID || data.effectID == "BURN_FIRE")
                {
                    while (data.tickElapsed >= TickInterval)
                    {
                        data.tickElapsed -= TickInterval;
                        ApplyTick(target, data);
                    }
                }

                if (data.remainingTime <= 0f)
                    RemoveEffect(target, data.effectID);
            }
        }
    }

    private static bool CanApplyGameplay(GameObject target)
    {
        PlayerCore core = target.GetComponent<PlayerCore>();
        return core == null || (core.IsSpawned && core.IsOwner);
    }

    private static void ApplyOnStart(
        GameObject target,
        EffectData data)
    {
        if (!CanApplyGameplay(target))
            return;

        PlayerCore core = target.GetComponent<PlayerCore>();

        PlayerVitals vitals = core != null
            ? core.playerVitals
            : target.GetComponent<PlayerVitals>();

        PlayerStats stats = core != null
            ? core.playerStats
            : target.GetComponent<PlayerStats>();

        int amount = Mathf.RoundToInt(data.value);

        switch (data.effectID)
        {
            case HealInstantID:
                if (vitals != null)
                    vitals.HealActiveCharacter(amount);

                SoundEffectManager.Play("Use Pot");
                break;

            case ManaInstantID:
                if (vitals != null)
                    vitals.RecoverMPActiveCharacter(amount);
                break;

            case BuffSTRID:
                if (stats != null)
                    stats.ModifyEffectStat("STR", amount);
                break;

            case BuffDEXID:
                if (stats != null)
                    stats.ModifyEffectStat("DEX", amount);
                break;

            case DebuffDEXID:
                if (stats != null)
                    stats.ModifyEffectStat("DEX", -amount);
                break;

            case BurnFireID:
                if (vitals != null)
                    vitals.TakeDamage(amount, canDodge: false);
                break;

            case CloverBuffID:
                if (stats != null)
                {
                    stats.ModifyEffectCritical(
                        data.value * 100f,
                        data.secondaryValue
                    );
                }
                break;

            case FeatherBuffID:
                if (stats != null)
                {
                    stats.ModifyEffectMobility(
                        data.value,
                        data.secondaryValue * 100f
                    );
                }
                break;
        }
    }

    private static void ApplyTick(GameObject target, EffectData data)
    {
        if (!CanApplyGameplay(target)) return;

        PlayerCore core = target.GetComponent<PlayerCore>();
        PlayerVitals vitals = core != null ? core.playerVitals : target.GetComponent<PlayerVitals>();
        if (vitals == null) return;

        switch (data.effectID)
        {
            case HealRegenID:
                {
                    if (core == null || core.playerStats == null || core.classController == null ||
                        vitals.netIsDead.Value || vitals.isProcessingDeath || vitals.isGameOver)
                        return;

                    bool isKnight = core.classController.IsKnightActive;
                    int maxHP = isKnight ? core.playerStats.finalKnightMaxHP : core.playerStats.finalMageMaxHP;
                    int currentHP = isKnight ? vitals.netKnightHealth.Value : vitals.netMageHealth.Value;
                    if (maxHP <= 0 || currentHP <= 0 || currentHP >= maxHP) return;

                    int heal = Mathf.Max(1, Mathf.CeilToInt(maxHP * Mathf.Clamp01(data.value)));
                    vitals.HealActiveCharacter(heal);
                    break;
                }
            case "BURN_FIRE":
                vitals.TakeDamage(
                    Mathf.RoundToInt(data.value),
                    canDodge: false
                );
                break;
        }
    }

    private static void UndoOnEnd(GameObject target, EffectData data)
    {
        if (!CanApplyGameplay(target)) return;
        PlayerCore core = target.GetComponent<PlayerCore>();
        PlayerStats stats = core != null ? core.playerStats : target.GetComponent<PlayerStats>();
        if (stats == null) return;

        int amount = Mathf.RoundToInt(data.value);
        switch (data.effectID)
        {
            case "BUFF_STR": stats.ModifyEffectStat("STR", -amount); break;
            case "BUFF_DEX": stats.ModifyEffectStat("DEX", -amount); break;
            case "DEBUFF_DEX": stats.ModifyEffectStat("DEX", amount); break;
            case CloverBuffID:
                stats.ModifyEffectCritical(
                    -data.value * 100f,
                    -data.secondaryValue
                );
                break;

            case FeatherBuffID:
                stats.ModifyEffectMobility(
                    -data.value,
                    -data.secondaryValue * 100f
                );
                break;
        }
    }
}

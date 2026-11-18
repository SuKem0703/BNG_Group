using UnityEngine;
using System.Collections.Generic;

public class EffectUIAdapter : MonoBehaviour
{
    [Header("UI Container")]
    public Transform effectGrid;

    [Header("Effect Prefabs")]
    public List<Effect> effectPrefabs;

    private readonly Dictionary<string, Effect> icons = new();
    private bool subscribed;

    private void OnEnable()
    {
        PlayerCore.OnPlayerSpawned += OnPlayerSpawned;
        Subscribe();
        RefreshActiveEffects();
    }

    private void Start()
    {
        Subscribe();
        RefreshActiveEffects();
    }

    private void OnDisable()
    {
        PlayerCore.OnPlayerSpawned -= OnPlayerSpawned;
        if (subscribed && EffectService.Instance != null)
        {
            EffectService.Instance.OnEffectAdded -= DrawEffectUI;
            EffectService.Instance.OnEffectRemoved -= RemoveEffectUI;
        }
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || EffectService.Instance == null) return;
        EffectService.Instance.OnEffectAdded += DrawEffectUI;
        EffectService.Instance.OnEffectRemoved += RemoveEffectUI;
        subscribed = true;
    }

    private void OnPlayerSpawned(PlayerCore _) => RefreshActiveEffects();

    private void RefreshActiveEffects()
    {
        if (PlayerCore.Instance == null || EffectService.Instance == null) return;
        foreach (var data in EffectService.Instance.GetEffects(PlayerCore.Instance.gameObject))
            DrawEffectUI(PlayerCore.Instance.gameObject, data);
    }

    private static bool IsLocalPlayer(GameObject target)
    {
        return target != null && PlayerCore.Instance != null &&
               target == PlayerCore.Instance.gameObject;
    }

    private void DrawEffectUI(GameObject target, EffectService.EffectData data)
    {
        if (effectGrid == null || !IsLocalPlayer(target)) return;

        if (icons.TryGetValue(data.effectID, out Effect oldIcon))
        {
            if (oldIcon != null) Destroy(oldIcon.gameObject);
            icons.Remove(data.effectID);
        }

        Effect prefab = effectPrefabs.Find(p => p != null && p.effectID == data.effectID);
        if (prefab == null)
        {
            Debug.LogWarning($"[EffectUIAdapter] Missing icon prefab: {data.effectID}");
            return;
        }

        Effect icon = Instantiate(prefab, effectGrid);
        icon.Initialize(target, data);
        icons[data.effectID] = icon;
    }

    private void RemoveEffectUI(GameObject target, string effectID)
    {
        if (!IsLocalPlayer(target)) return;
        if (!icons.TryGetValue(effectID, out Effect icon)) return;
        if (icon != null) Destroy(icon.gameObject);
        icons.Remove(effectID);
    }
}

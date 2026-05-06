using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EffectUIAdapter : MonoBehaviour
{
    [Header("UI Container")]
    public Transform effectGrid;

    [Header("Effect Prefabs")]
    public List<Effect> effectPrefabs;

    private GameObject localPlayer;

    private void Start()
    {
        localPlayer = GameObject.FindGameObjectWithTag("PlayerController");

        if (EffectService.Instance != null)
        {
            EffectService.Instance.OnEffectAdded += DrawEffectUI;
        }
    }

    private void OnDestroy()
    {
        if (EffectService.Instance != null)
        {
            EffectService.Instance.OnEffectAdded -= DrawEffectUI;
        }
    }

    private void DrawEffectUI(GameObject target, EffectService.EffectData effectData)
    {
        if (effectGrid == null) return;

        if (target != localPlayer) return;

        GameObject prefab = GetPrefab(effectData.effectID);
        if (prefab == null)
        {
            Debug.LogWarning($"Không có prefab cho effectID: {effectData.effectID}");
            return;
        }

        GameObject newEff = Instantiate(prefab, effectGrid);
        Effect effUI = newEff.GetComponent<Effect>();

        if (effUI != null)
        {
            effUI.Initialize(target, effectData.duration, effectData.value);
        }
    }

    private GameObject GetPrefab(string id)
    {
        Effect prefab = effectPrefabs.FirstOrDefault(p => p.effectID == id);
        return (prefab != null) ? prefab.gameObject : null;
    }
}
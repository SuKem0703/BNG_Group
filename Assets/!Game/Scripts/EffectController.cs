using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EffectController : MonoBehaviour
{
    public static EffectController Instance;

    [Header("UI Container")]
    public Transform effectGrid;

    [Header("Effect Prefabs")]
    public List<Effect> effectPrefabs;

    private Dictionary<GameObject, List<Effect>> activeEffects = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        if (effectGrid == null)
            effectGrid = GameObject.Find("GameUI/CommonUI/UI_EffectGrid")?.transform;
    }
    public void AddEffect(GameObject target, string effectID, float duration, float value)
    {
        if (effectGrid == null) return;

        GameObject prefab = GetPrefab(effectID);
        if (prefab == null)
        {
            Debug.LogWarning($"Không có prefab cho effectID: {effectID}");
            return;
        }

        GameObject newEff = Instantiate(prefab, effectGrid);
        Effect eff = newEff.GetComponent<Effect>();

        if (eff != null)
        {
            eff.Initialize(target, duration, value);
        }

        if (!activeEffects.ContainsKey(target))
            activeEffects[target] = new List<Effect>();
        activeEffects[target].Add(eff);
    }

    private GameObject GetPrefab(string id)
    {
        Effect prefab = effectPrefabs.FirstOrDefault(p => p.effectID == id);
        return (prefab != null) ? prefab.gameObject : null;
    }
}
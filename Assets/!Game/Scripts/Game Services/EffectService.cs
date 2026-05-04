using UnityEngine;
using System.Collections.Generic;
using System;

public class EffectService : MonoBehaviour
{
    public static EffectService Instance { get; private set; }

    private Dictionary<GameObject, List<EffectData>> activeEffects = new();

    public event Action<GameObject, EffectData> OnEffectAdded;
    public event Action<GameObject, string> OnEffectRemoved;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void AddEffect(GameObject target, string effectID, float duration, float value)
    {
        if (!activeEffects.ContainsKey(target))
            activeEffects[target] = new List<EffectData>();

        EffectData newEffect = new EffectData(effectID, duration, value);
        activeEffects[target].Add(newEffect);

        OnEffectAdded?.Invoke(target, newEffect);
    }

    public class EffectData
    {
        public string effectID;
        public float duration;
        public float value;

        public EffectData(string id, float dur, float val)
        {
            effectID = id; duration = dur; value = val;
        }
    }
}
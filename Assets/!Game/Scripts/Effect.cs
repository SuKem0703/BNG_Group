using UnityEngine;
using UnityEngine.UI;

// Presentation only. Gameplay is handled by EffectService.
public class Effect : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Image overlayImage;

    [Header("Effect Settings")]
    public string effectID;
    public float duration;
    public float value;

    private EffectService.EffectData runtimeData;

    public void Initialize(GameObject target, EffectService.EffectData data)
    {
        runtimeData = data;
        effectID = data.effectID;
        duration = data.duration;
        value = data.value;
        RefreshFill();
    }

    private void Update()
    {
        if (runtimeData == null || runtimeData.remainingTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        RefreshFill();
    }

    private void RefreshFill()
    {
        if (overlayImage != null && duration > 0f)
            overlayImage.fillAmount = 1f - Mathf.Clamp01(runtimeData.remainingTime / duration);
    }
}

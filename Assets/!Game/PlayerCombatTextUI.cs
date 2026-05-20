using UnityEngine;

public class PlayerCombatTextUI : MonoBehaviour
{
    [SerializeField] private PlayerCore core;

    private void OnEnable()
    {
        if (core.playerVitals != null)
        {
            core.playerVitals.OnDamaged += ShowDamagePopup;
            core.playerVitals.OnHealed += ShowHealPopup;
        }
    }

    private void OnDisable()
    {
        if (core.playerVitals != null)
        {
            core.playerVitals.OnDamaged -= ShowDamagePopup;
            core.playerVitals.OnHealed -= ShowHealPopup;
        }
    }

    private void ShowDamagePopup(int amount, DamageSourceType type)
    {
        if (DamagePopupPool.Instance == null) return;
        Vector3 pos = transform.position + new Vector3(0, 1f, 0);
        DamagePopupPool.Instance.GetPopup(pos).Setup(amount, type);
    }

    private void ShowHealPopup(int amount, DamageSourceType type)
    {
        if (DamagePopupPool.Instance == null) return;
        Transform activeTransform = core.classController.IsKnightActive ? core.classController.knightObject.transform : core.classController.mageObject.transform;
        Vector3 pos = activeTransform.position + new Vector3(0, 1.5f, 0);
        DamagePopupPool.Instance.GetPopup(pos).Setup(amount, type);
    }
}
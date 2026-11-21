
using UnityEngine;

public class PlayerCombatTextUI : MonoBehaviour
{
    [SerializeField] private PlayerCore core;

    private void OnEnable()
    {
        if (core == null || core.playerVitals == null)
            return;

        core.playerVitals.OnDamaged += ShowDamagePopup;
        core.playerVitals.OnHealed += ShowHealPopup;
        core.playerVitals.OnDodged += ShowDodgePopup;
    }

    private void OnDisable()
    {
        if (core == null || core.playerVitals == null)
            return;

        core.playerVitals.OnDamaged -= ShowDamagePopup;
        core.playerVitals.OnHealed -= ShowHealPopup;
        core.playerVitals.OnDodged -= ShowDodgePopup;
    }

    private void ShowDamagePopup(
        int amount,
        DamageSourceType type)
    {
        if (DamagePopupPool.Instance == null)
            return;

        Vector3 pos = transform.position +
                      new Vector3(0, 1f, 0);

        DamagePopupPool.Instance
            .GetPopup(pos)
            .Setup(amount, type);
    }

    private void ShowHealPopup(
        int amount,
        DamageSourceType type)
    {
        if (DamagePopupPool.Instance == null)
            return;

        Vector3 pos = GetActiveCharacterPosition() +
                      new Vector3(0, 1.5f, 0);

        DamagePopupPool.Instance
            .GetPopup(pos)
            .Setup(amount, type);
    }

    private void ShowDodgePopup()
    {
        if (DamagePopupPool.Instance == null)
            return;

        Vector3 pos = GetActiveCharacterPosition() +
                      new Vector3(0, 1.5f, 0);

        DamagePopupPool.Instance
            .GetPopup(pos)
            .SetupDodge();
    }

    private Vector3 GetActiveCharacterPosition()
    {
        if (core == null || core.classController == null)
            return transform.position;

        GameObject activeCharacter =
            core.classController.IsKnightActive
                ? core.classController.knightObject
                : core.classController.mageObject;

        return activeCharacter != null
            ? activeCharacter.transform.position
            : transform.position;
    }
}

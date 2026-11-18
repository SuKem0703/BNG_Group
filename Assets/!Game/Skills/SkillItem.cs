
using UnityEngine;
using UnityEngine.UI;

public class SkillItem : MonoBehaviour
{
    [Header("Skill")]
    public string skillID;
    public Image icon;

    [Header("Cooldown UI")]
    [SerializeField] private Image cooldownOverlay;

    public void Setup(SkillData data)
    {
        if (data == null)
            return;

        skillID = data.skillID;

        if (icon != null)
            icon.sprite = data.skillIcon;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.sprite = data.skillIcon;

            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod =
                Image.FillMethod.Radial360;

            cooldownOverlay.fillOrigin =
                (int)Image.Origin360.Top;

            cooldownOverlay.fillClockwise = false;
            cooldownOverlay.raycastTarget = false;
            cooldownOverlay.fillAmount = 0f;
        }

        RefreshCooldown();
    }

    private void Update()
    {
        RefreshCooldown();
    }

    private void RefreshCooldown()
    {
        if (cooldownOverlay == null)
            return;

        PlayerCore player = PlayerCore.Instance;

        if (player == null ||
            player.playerVFX == null ||
            string.IsNullOrEmpty(skillID))
        {
            cooldownOverlay.fillAmount = 0f;
            return;
        }

        cooldownOverlay.fillAmount =
            player.playerVFX.GetCooldownFill(skillID);
    }
}

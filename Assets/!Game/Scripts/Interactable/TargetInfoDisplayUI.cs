using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TargetInfoDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel cha chứa tất cả thông tin")]
    public GameObject infoPanel;

    [Tooltip("Text hiển thị tên (NPC, Item, Enemy)")]
    public TextMeshProUGUI nameText;

    [Tooltip("Text hiển thị hành động ([F] Nói chuyện, [F] Nhặt)")]
    public TextMeshProUGUI actionText;

    [Tooltip("Image hiển thị Portrait/Icon")]
    public Image portraitImage;

    private IInteractable currentInteractTarget;
    private Enemy currentEnemyTarget;

    private void Awake()
    {
        if (infoPanel == null) infoPanel = transform.FindDeepChild("InfoPanel")?.gameObject;
        if (nameText == null) nameText = transform.FindDeepChild("NameText")?.GetComponent<TextMeshProUGUI>();
        if (actionText == null) actionText = transform.FindDeepChild("ActionText")?.GetComponent<TextMeshProUGUI>();
        if (portraitImage == null) portraitImage = transform.FindDeepChild("PortraitImage")?.GetComponent<Image>();
    }

    private void OnEnable()
    {
        InteractionDetector.OnTargetChanged += HandleInteractTargetChanged;
        CombatTargetSelector.OnEnemyTargetChanged += HandleEnemyTargetChanged;

        if (infoPanel != null) infoPanel.SetActive(false);
    }

    private void OnDisable()
    {
        InteractionDetector.OnTargetChanged -= HandleInteractTargetChanged;
        CombatTargetSelector.OnEnemyTargetChanged -= HandleEnemyTargetChanged;
    }

    private void HandleInteractTargetChanged(IInteractable newTarget)
    {
        currentInteractTarget = newTarget;
        RefreshUI();
    }

    private void HandleEnemyTargetChanged(Enemy newEnemy)
    {
        currentEnemyTarget = newEnemy;
        RefreshUI();
    }

    private void RefreshUI()
    {
        ITargetableInfo infoSource = null;

        if (currentEnemyTarget != null && !currentEnemyTarget.IsDead)
        {
            infoSource = currentEnemyTarget as ITargetableInfo;
        }
        else if (currentInteractTarget != null)
        {
            infoSource = currentInteractTarget as ITargetableInfo;
        }

        if (infoSource != null)
        {
            TargetInfoData info = infoSource.GetInfo();

            if (nameText != null)
            {
                nameText.text = info.name;
                nameText.color = RarityColorHelper.GetColorByRarity(info.rarity);
            }

            if (portraitImage != null)
            {
                if (info.portrait != null)
                {
                    portraitImage.gameObject.SetActive(true);
                    portraitImage.sprite = info.portrait;
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }

            switch (info.type)
            {
                case TargetType.NPC:
                case TargetType.Item:
                    if (actionText != null)
                    {
                        actionText.gameObject.SetActive(true);
                        actionText.text = $"[F] {info.actionText}";
                    }
                    break;

                case TargetType.Enemy:
                    if (actionText != null)
                    {
                        actionText.gameObject.SetActive(true);
                        actionText.text = info.actionText;
                    }
                    break;

                default:
                    if (actionText != null) actionText.gameObject.SetActive(false);
                    break;
            }

            if (infoPanel != null) infoPanel.SetActive(true);
        }
        else
        {
            if (infoPanel != null) infoPanel.SetActive(false);
        }
    }
}
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

    [Header("Rarity Frames")]
    [Tooltip("Image cho khung icon (vuông, giống ItemIconCard)")]
    public Image iconCard;

    private void Awake()
    {
        if (infoPanel == null)
        {
            infoPanel = transform.FindDeepChild("InfoPanel")?.gameObject;
        }
        if (nameText == null)
        {
            nameText = transform.FindDeepChild("NameText")?.GetComponent<TextMeshProUGUI>();
        }
        if (actionText == null)
        {
            actionText = transform.FindDeepChild("ActionText")?.GetComponent<TextMeshProUGUI>();
        }
        if (portraitImage == null)
        {
            portraitImage = transform.FindDeepChild("PortraitImage")?.GetComponent<Image>();
        }
        if (iconCard == null)
        {
            iconCard = transform.FindDeepChild("ItemIconCard")?.GetComponent<Image>();
        }
    }
    private void OnEnable()
    {
        InteractionDetector.OnTargetChanged += HandleTargetChanged;
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    private void OnDisable()
    {
        InteractionDetector.OnTargetChanged -= HandleTargetChanged;
    }
    private void HandleTargetChanged(IInteractable newTarget)
    {
        if (newTarget == null)
        {
            if (infoPanel != null) infoPanel.SetActive(false);
            return;
        }

        if (newTarget is ITargetableInfo targetInfo)
        {
            TargetInfoData info = targetInfo.GetInfo();

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

            string rarityNameStr = info.rarity.ToString();

            if (iconCard != null)
            {
                string cardPath = $"Square Card/{rarityNameStr}";
                Sprite iconSprite = Resources.Load<Sprite>(cardPath);
                if (iconSprite != null)
                {
                    iconCard.sprite = iconSprite;
                    iconCard.gameObject.SetActive(true); // Bật lên
                }
                else
                {
                    Debug.LogWarning($"[TargetInfoDisplayUI] Không tìm thấy sprite cho item icon tại đường dẫn: {cardPath}");
                    iconCard.gameObject.SetActive(false); // Ẩn đi nếu không tìm thấy
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
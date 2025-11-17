using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableTooltip : MonoBehaviour
{
    public static ConsumableTooltip Instance;

    [Header("Cấu trúc UI")]
    [SerializeField] private Image backGround;
    [SerializeField] private Image borderFrame;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image itemPortrait;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text quantityText;

    private RectTransform tooltipRect;
    private RectTransform canvasRect;

    void Awake()
    {
        Instance = this;

        Transform t = transform;

        backGround = t.FindDeepChild("backGround")?.GetComponent<Image>();
        borderFrame = t.FindDeepChild("borderFrame")?.GetComponent<Image>();
        nameText = t.FindDeepChild("nameText")?.GetComponent<TextMeshProUGUI>();
        itemPortrait = t.FindDeepChild("itemPortrait")?.GetComponent<Image>();
        descriptionText = t.FindDeepChild("descriptionText")?.GetComponent<TextMeshProUGUI>();
        quantityText = t.FindDeepChild("quantityText")?.GetComponent<TextMeshProUGUI>();

        tooltipRect = GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    public void Show(Item item)
    {
        if (item == null) return;
        gameObject.SetActive(true);

        // --- Load khung phẩm ---
        ItemRarity rarity = item.rarity;
        string path = $"Vertical Card/{rarity}";
        Sprite raritySprite = Resources.Load<Sprite>(path);
        if (raritySprite != null)
            borderFrame.sprite = raritySprite;
        else
            Debug.LogWarning($"Không tìm thấy sprite cho rarity '{rarity}' tại: {path}");

        // --- Hiển thị nội dung ---
        nameText.text = item.Name;
        backGround.color = RarityColorHelper.GetColorByRarity(item.rarity);
        itemPortrait.sprite = item.icon;
        descriptionText.text = item.description;
        quantityText.text = $"SỐ LƯỢNG: {item.quantity}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        // Pivot: góc dưới phải trùng chuột
        tooltipRect.pivot = new Vector2(1f, 0f);

        Vector2 mousePos = Input.mousePosition;

        // Offset nhẹ sang trái và lên trên
        Vector2 offset = new Vector2(-10f, 10f);
        mousePos += offset;

        // Convert sang vị trí local trong Canvas
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out anchoredPos);

        // ⚙️ Kích thước thật của tooltip (600x800, scale 0.75)
        float tooltipWidth = 600f * 0.6f;
        float tooltipHeight = 800f * 0.6f;

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // Clamp để tooltip không bị tràn khỏi màn hình
        float minX = -canvasWidth / 2f + tooltipWidth;
        float maxX = canvasWidth / 2f;
        float minY = -canvasHeight / 2f;
        float maxY = canvasHeight / 2f - tooltipHeight;

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        tooltipRect.anchoredPosition = anchoredPos;
    }
}

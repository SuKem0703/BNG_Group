using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class PotentialStatsDisplay : MonoBehaviour
{
    private PlayerStats playerStats;
    private SaveController saveController;

    [Header("Tiềm năng")]
    public TMP_Text availablePointsText;

    [Header("Stats Bonus")]
    public TMP_Text physAttText;
    public TMP_Text magicAttText;
    public TMP_Text defAttText;

    [Header("Nút cộng")]
    public Button strAddButton;
    public Button dexAddButton;
    public Button conAddButton;
    public Button intAddButton;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        saveController = FindFirstObjectByType<SaveController>();

        if (playerStats == null)
        {
            Debug.LogError("PlayerStats vẫn null sau khi chờ trong PotentialStatsDisplay!");
            return;
        }

        if (saveController == null)
        {
            Debug.LogError("SaveController vẫn null sau khi chờ trong PotentialStatsDisplay!");
            return;
        }

        strAddButton.onClick.AddListener(() => IncreaseStat("STR"));
        dexAddButton.onClick.AddListener(() => IncreaseStat("DEX"));
        conAddButton.onClick.AddListener(() => IncreaseStat("CON"));
        intAddButton.onClick.AddListener(() => IncreaseStat("INT"));

        AddTooltipEvent(strAddButton.gameObject, "Tăng sức mạnh vật lý và chí mạng.");
        AddTooltipEvent(dexAddButton.gameObject, "Tăng tốc độ di chuyển và khả năng chống chịu.");
        AddTooltipEvent(conAddButton.gameObject, "Tăng thể lực và khả năng hồi phục.");
        AddTooltipEvent(intAddButton.gameObject, "Tăng sức mạnh phép thuật và năng lượng.");

    }
    private void Start()
    {
        tooltipPanel.SetActive(false);

    }
    private void OnEnable()
    {
        Hide();
    }
    void Update()
    {
        if (playerStats == null) return;

        // Tiềm năng
        availablePointsText.text = $"{playerStats.potentialPoints}";

        // Chỉ số bonus
        physAttText.text = $"+ {playerStats.basePhysicalAttack} ";
        magicAttText.text = $"+ {playerStats.baseMagicAttack} ";
        defAttText.text = $" + {playerStats.baseDefense} ";

        // Hiển thị/ẩn nút cộng tùy theo điểm còn lại
        bool canIncrease = playerStats.potentialPoints > 0;
        strAddButton.gameObject.SetActive(canIncrease);
        dexAddButton.gameObject.SetActive(canIncrease);
        conAddButton.gameObject.SetActive(canIncrease);
        intAddButton.gameObject.SetActive(canIncrease);

        // Tooltip follow chuột
        if (tooltipPanel.activeSelf)
        {
            tooltipPanel.transform.position = Input.mousePosition;
        }
    }

    void IncreaseStat(string statName)
    {
        if (playerStats.potentialPoints <= 0)
        {
            Debug.LogWarning("Không còn điểm tiềm năng!");
            return;
        }

        switch (statName)
        {
            case "STR":
                playerStats.STR++;
                Debug.Log("Tăng STR lên " + playerStats.STR);
                break;
            case "DEX":
                playerStats.DEX++;
                Debug.Log("Tăng DEX lên " + playerStats.DEX);
                break;
            case "CON":
                playerStats.CON++;
                Debug.Log("Tăng CON lên " + playerStats.CON);
                break;
            case "INT":
                playerStats.INT++;
                Debug.Log("Tăng INT lên " + playerStats.INT);
                break;
            default:
                Debug.LogError("Stat không hợp lệ: " + statName);
                return;
        }
        playerStats.potentialPoints--;
        saveController.SaveGame();
    }
    public void ResetPoints()
    {
        if (playerStats == null || playerStats.gem < 20)
        {
            Debug.LogWarning("Không đủ gem để reset điểm tiềm năng!");
            return;
        }
        playerStats.SpendGem(20);
        playerStats.ResetPotential();
        
        saveController.SaveGame();

        playerStats.ApplyEquippedItems();
    }

    // === Hover Tooltip Setup ===
    void AddTooltipEvent(GameObject target, string message)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        // OnPointerEnter
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) =>
        {
            tooltipText.text = message;
            tooltipPanel.SetActive(true);

            // ===== Logic positioning giống Update bạn đưa =====
            RectTransform canvasRect = tooltipPanel.transform.root.GetComponent<Canvas>().GetComponent<RectTransform>();
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();

            // Pivot: góc dưới phải trùng chuột
            tooltipRect.pivot = new Vector2(1f, 0f);

            Vector2 mousePos = Input.mousePosition;

            // Offset nhẹ sang trái + lên trên
            Vector2 offset = new Vector2(-10f, 10f);
            mousePos += offset;

            // Convert sang local pos trong canvas
            Vector2 anchoredPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out anchoredPos);

            // Tooltip size
            float tooltipWidth = 1000;
            float tooltipHeight = 50;

            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            float minX = -canvasWidth / 2f + tooltipWidth;
            float maxX = canvasWidth / 2f;
            float minY = -canvasHeight / 2f;
            float maxY = canvasHeight / 2f - tooltipHeight;

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

            tooltipRect.anchoredPosition = anchoredPos;
        });
        trigger.triggers.Add(entryEnter);

        // OnPointerExit
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((eventData) =>
        {
            tooltipText.text = "";
            tooltipPanel.SetActive(false);
        });
        trigger.triggers.Add(entryExit);
    }
    public void Hide()
    {
        tooltipText.text = "";
        tooltipPanel.SetActive(false);
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class PotentialUIAdapter : MonoBehaviour
{
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

    [Header("Màu sắc khi Hover")]
    public Color strHoverColor = new Color(1f, 0.5f, 0f); // Màu Cam (Sát thương vật lý)
    public Color dexHoverColor = Color.green;             // Màu Xanh lá (Nhanh nhẹn)
    public Color conHoverColor = Color.red;               // Màu Đỏ (Máu/Thể lực)
    public Color intHoverColor = new Color(0f, 0.8f, 1f); // Màu Xanh dương nhạt (Phép thuật)

    [Header("Nút Reset")]
    public Button resetButton;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    private GameObject ConfirmUIPrefab => LoadResourceManager.Instance.ConfirmUIPrefab;

    private PlayerStats playerStats
    {
        get
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var adapter = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerCore>();
                if (adapter != null) return adapter.playerStats;
            }
            return FindFirstObjectByType<PlayerStats>();
        }
    }

    void Awake()
    {
        strAddButton.onClick.AddListener(() => OnAddClicked("STR"));
        dexAddButton.onClick.AddListener(() => OnAddClicked("DEX"));
        conAddButton.onClick.AddListener(() => OnAddClicked("CON"));
        intAddButton.onClick.AddListener(() => OnAddClicked("INT"));

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        SetupTooltips();
    }

    private void OnEnable()
    {
        HideTooltip();

        if (PotentialService.Instance != null)
        {
            PotentialService.Instance.OnPendingPointsChanged += UpdateUI;
        }

        UpdateUI();
    }

    private void OnDisable()
    {
        if (PotentialService.Instance != null)
        {
            PotentialService.Instance.OnPendingPointsChanged -= UpdateUI;
        }
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            HandleTooltipPosition();
        }
    }

    private void UpdateUI()
    {
        if (playerStats == null || PotentialService.Instance == null) return;

        int totalPending = PotentialService.Instance.GetTotalPending();
        int currentAvailable = playerStats.potentialPoints - totalPending;

        availablePointsText.text = $"{currentAvailable}";

        physAttText.text = $"+ {playerStats.basePhysicalAttack} ";
        magicAttText.text = $"+ {playerStats.baseMagicAttack} ";
        defAttText.text = $" + {playerStats.baseDefense} ";

        bool canIncrease = currentAvailable > 0;
        strAddButton.gameObject.SetActive(canIncrease);
        dexAddButton.gameObject.SetActive(canIncrease);
        conAddButton.gameObject.SetActive(canIncrease);
        intAddButton.gameObject.SetActive(canIncrease);
    }

    private void OnAddClicked(string stat)
    {
        if (playerStats == null) return;
        PotentialService.Instance.IncreaseStat(stat, playerStats.potentialPoints);
    }

    private void OnResetClicked()
    {
        if (PlayerWallet.Instance == null) return;

        if (PlayerWallet.Instance.gem < 20)
        {
            GameNotify.Show("Bạn không đủ 20 Gem để reset điểm!");
            return;
        }

        if (ConfirmUIPrefab == null)
        {
            Debug.LogError("ConfirmUIPrefab NOT FOUND!");
            return;
        }

        GameObject confirmUIObj = Instantiate(ConfirmUIPrefab);
        var confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        if (confirmUI != null)
        {
            confirmUI.Show("Bạn có chắc muốn Reset toàn bộ điểm tiềm năng với giá <color=yellow>20 Gem</color>?", ExecuteReset);
        }
    }

    private void ExecuteReset()
    {
        if (resetButton != null) resetButton.interactable = false;

        PotentialService.Instance.RequestResetStats((success) =>
        {
            if (resetButton != null) resetButton.interactable = true;

            if (success)
            {
                playerStats.ApplyEquippedItems();
                UpdateUI();
                GameNotify.Show("Reset điểm thành công!");
            }
            else
            {
                GameNotify.Show("Reset thất bại! Vui lòng thử lại.");
            }
        });
    }

    void HandleTooltipPosition()
    {
        tooltipPanel.transform.position = Input.mousePosition;
    }

    void SetupTooltips()
    {
        AddTooltipEvent(strAddButton.gameObject, "Tăng sức mạnh vật lý và chí mạng.", strHoverColor);
        AddTooltipEvent(dexAddButton.gameObject, "Tăng tốc độ di chuyển và khả năng chống chịu.", dexHoverColor);
        AddTooltipEvent(conAddButton.gameObject, "Tăng thể lực và khả năng hồi phục.", conHoverColor);
        AddTooltipEvent(intAddButton.gameObject, "Tăng sức mạnh phép thuật và năng lượng.", intHoverColor);
    }

    void AddTooltipEvent(GameObject target, string message, Color tooltipColor)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) =>
        {
            if (tooltipText != null) tooltipText.color = tooltipColor;

            tooltipText.text = message;
            tooltipPanel.SetActive(true);
            Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipRect.pivot = new Vector2(1f, 0f);

            Vector2 mousePos = Input.mousePosition;
            mousePos += new Vector2(-10f, 10f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out Vector2 anchoredPos);

            float tooltipWidth = 200;
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

        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };

        entryExit.callback.AddListener((eventData) => { HideTooltip(); });

        trigger.triggers.Add(entryExit);
    }

    public void HideTooltip()
    {
        if (tooltipText != null) tooltipText.text = "";
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}
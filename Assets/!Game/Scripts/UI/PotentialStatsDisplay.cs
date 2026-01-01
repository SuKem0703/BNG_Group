using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class PotentialStatsDisplay : MonoBehaviour
{
    private PlayerStats playerStats;

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

    [Header("Nút Reset")]
    public Button resetButton;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    private Dictionary<string, int> pendingPoints = new Dictionary<string, int>();

    private int localPotentialPoints;
    private int localStr, localDex, localCon, localInt;

    private Coroutine batchSendCoroutine;
    private float debounceTime = 1.5f;
    private bool isDirty = false;

    private GameObject ConfirmUIPrefab => LoadResourceManager.Instance.ConfirmUIPrefab;
    private GameObject NotifyUIPrefab => LoadResourceManager.Instance.NotifyUIPrefab;
    void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;

        // Init Dictionary
        pendingPoints["STR"] = 0;
        pendingPoints["DEX"] = 0;
        pendingPoints["CON"] = 0;
        pendingPoints["INT"] = 0;

        strAddButton.onClick.AddListener(() => OnClickIncrease("STR"));
        dexAddButton.onClick.AddListener(() => OnClickIncrease("DEX"));
        conAddButton.onClick.AddListener(() => OnClickIncrease("CON"));
        intAddButton.onClick.AddListener(() => OnClickIncrease("INT"));

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetPoints);

        SetupTooltips();
    }

    private void OnEnable()
    {
        HideTooltip();
        if (PlayerStatsService.Instance != null)
        {
            // Sync dữ liệu mới nhất từ server về
            PlayerStatsService.Instance.SyncProfile((success) =>
            {
                if (success) InitializeLocalValues();
            });
        }
        else
        {
            InitializeLocalValues();
        }
    }

    private void OnDisable()
    {
        // Nếu đóng bảng mà vẫn còn điểm chưa gửi -> Gửi ngay lập tức
        if (isDirty)
        {
            SendBatchRequestNow();
        }
    }

    // Khởi tạo giá trị local từ PlayerStats thật
    void InitializeLocalValues()
    {
        if (playerStats == null) return;

        localPotentialPoints = playerStats.potentialPoints;
        localStr = playerStats.STR;
        localDex = playerStats.DEX;
        localCon = playerStats.CON;
        localInt = playerStats.INT;

        pendingPoints["STR"] = 0;
        pendingPoints["DEX"] = 0;
        pendingPoints["CON"] = 0;
        pendingPoints["INT"] = 0;

        isDirty = false;
    }

    void Update()
    {
        if (playerStats == null) return;

        availablePointsText.text = $"{localPotentialPoints}";

        physAttText.text = $"+ {playerStats.basePhysicalAttack} ";
        magicAttText.text = $"+ {playerStats.baseMagicAttack} ";
        defAttText.text = $" + {playerStats.baseDefense} ";

        // Kiểm tra nút bấm dựa trên điểm Local
        bool canIncrease = localPotentialPoints > 0;
        strAddButton.gameObject.SetActive(canIncrease);
        dexAddButton.gameObject.SetActive(canIncrease);
        conAddButton.gameObject.SetActive(canIncrease);
        intAddButton.gameObject.SetActive(canIncrease);

        HandleTooltipPosition();
    }

    // --- LOGIC CỘNG ĐIỂM (CLIENT SIDE PREDICTION) ---
    void OnClickIncrease(string statName)
    {
        if (localPotentialPoints <= 0) return;

        localPotentialPoints--;
        pendingPoints[statName]++;

        switch (statName)
        {
            case "STR": localStr++; break;
            case "DEX": localDex++; break;
            case "CON": localCon++; break;
            case "INT": localInt++; break;
        }

        isDirty = true;

        if (batchSendCoroutine != null) StopCoroutine(batchSendCoroutine);
        batchSendCoroutine = StartCoroutine(DebounceSend());
    }

    // Chờ người chơi dừng bấm thì mới gửi
    IEnumerator DebounceSend()
    {
        yield return new WaitForSeconds(debounceTime);
        SendBatchRequestNow();
    }

    // Gửi request thực sự
    void SendBatchRequestNow()
    {
        if (!isDirty) return;

        foreach (var kvp in pendingPoints)
        {
            string stat = kvp.Key;
            int amount = kvp.Value;

            if (amount > 0)
            {
                // Disable nút để tránh spam lúc đang gửi (Optional)
                // SetInteractable(false); 

                PlayerStatsService.Instance.DistributePoint(stat, amount, (success) =>
                {
                    if (success)
                    {

                    }
                    else
                    {
                        Debug.LogWarning("Lỗi đồng bộ! Hoàn trả lại điểm.");
                        InitializeLocalValues();
                    }
                });
            }
        }

        // Reset trạng thái
        pendingPoints["STR"] = 0;
        pendingPoints["DEX"] = 0;
        pendingPoints["CON"] = 0;
        pendingPoints["INT"] = 0;
        isDirty = false;
    }

    // --- LOGIC RESET ---
    public void ResetPoints()
    {
        if (batchSendCoroutine != null) StopCoroutine(batchSendCoroutine);
        InitializeLocalValues();

        if (playerStats == null) return;

        if (playerStats.gem < 20)
        {
            ShowNotification("Bạn không đủ 20 Gem để reset điểm!");
            return;
        }

        OpenResetConfirm();
    }

    // --- HÀM MỚI: Hiển thị Confirm UI ---
    private void OpenResetConfirm()
    {
        if (ConfirmUIPrefab == null)
        {
            Debug.LogError("ConfirmUIPrefab NOT FOUND!");
            return;
        }

        GameObject confirmUIObj = Instantiate(ConfirmUIPrefab);
        var confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        if (confirmUI != null)
        {
            string message = "Bạn có chắc muốn Reset toàn bộ điểm tiềm năng với giá <color=yellow>20 Gem</color>?";

            confirmUI.Show(message, () =>
            {
                ExecuteReset();
            });
        }
    }

    // Thực thi Reset sau khi đã Confirm
    private void ExecuteReset()
    {
        if (resetButton != null) resetButton.interactable = false;

        PlayerStatsService.Instance.ResetStats((success) =>
        {
            if (resetButton != null) resetButton.interactable = true;

            if (success)
            {
                InitializeLocalValues();
                playerStats.ApplyEquippedItems();

                ShowNotification("Reset điểm thành công!");
            }
            else
            {
                ShowNotification("Reset thất bại! Vui lòng thử lại.");
            }
        });
    }

    // Hiển thị Notify UI
    public void ShowNotification(string message)
    {
        if (NotifyUIPrefab == null) return;

        GameObject obj = Instantiate(NotifyUIPrefab);
        var controller = obj.GetComponent<NotifyUIController>();

        if (controller != null)
            controller.Show(message);
        else
            Destroy(obj);
    }

    void HandleTooltipPosition()
    {
        if (tooltipPanel.activeSelf)
            tooltipPanel.transform.position = Input.mousePosition;
    }

    void SetupTooltips()
    {
        AddTooltipEvent(strAddButton.gameObject, "Tăng sức mạnh vật lý và chí mạng.");
        AddTooltipEvent(dexAddButton.gameObject, "Tăng tốc độ di chuyển và khả năng chống chịu.");
        AddTooltipEvent(conAddButton.gameObject, "Tăng thể lực và khả năng hồi phục.");
        AddTooltipEvent(intAddButton.gameObject, "Tăng sức mạnh phép thuật và năng lượng.");
    }

    // === Hover Tooltip Setup (Giữ nguyên) ===
    void AddTooltipEvent(GameObject target, string message)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) =>
        {
            tooltipText.text = message;
            tooltipPanel.SetActive(true);
            Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipRect.pivot = new Vector2(1f, 0f);
            Vector2 mousePos = Input.mousePosition;
            Vector2 offset = new Vector2(-10f, 10f);
            mousePos += offset;
            Vector2 anchoredPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out anchoredPos);

            // Clamp logic... (Giữ nguyên logic clamp của bạn)
            float tooltipWidth = 200; // Sửa lại width hợp lý hơn
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
        entryExit.callback.AddListener((eventData) =>
        {
            HideTooltip();
        });
        trigger.triggers.Add(entryExit);
    }

    public void HideTooltip()
    {
        tooltipText.text = "";
        tooltipPanel.SetActive(false);
    }
}
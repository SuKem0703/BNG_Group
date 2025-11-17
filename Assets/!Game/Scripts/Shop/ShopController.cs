using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [Header("Cấu hình shop")]
    public GameObject shopPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public List<ShopItemData> shopItems;
    [SerializeField] private ShopItemPreview shopItemPreview;

    [Header("Prefabs")]
    public GameObject confirmUIPrefab;
    public GameObject notifyUIPrefab;

    private ItemDictionary itemDictionary;
    private CommonUIController commonUI;
    private PlayerStats playerStats;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI gemText;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();

        if (commonUI == null)
        {
			commonUI = FindObjectsByType<CommonUIController>(FindObjectsSortMode.None)
	.FirstOrDefault(x => x.name == "CommonUI");

			if (commonUI == null)
                Debug.LogError("CommonUIController not found!");
        }

        if (playerStats != null)
        {
            coinText = FindTextByName(transform, "CoinText");
            gemText = FindTextByName(transform, "GemText");
        }
        else
        {
            Debug.LogError("PlayerStats not found in ShopController!");
        }

        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        shopItemPreview = GetComponentsInChildren<ShopItemPreview>(true)
    .FirstOrDefault(x => x.name == "ItemPreview");

        confirmUIPrefab = Resources.Load<GameObject>("UI/ConfirmUICanvas");
        if (confirmUIPrefab == null)
        {
            Debug.LogError("Không tìm thấy 'ConfirmUICanvas' trong thư mục Resources!");
        }

        notifyUIPrefab = Resources.Load<GameObject>("UI/NotifyUICanvas");
        if (notifyUIPrefab == null)
        {
            Debug.LogError("Không tìm thấy 'NotifyUICanvas' trong thư mục Resources!");
        }
    }
    void Start()
    {
        StartCoroutine(SetupAndDeactivate());
    }

    IEnumerator SetupAndDeactivate()
    {
        yield return null;

        SetupShop();

        // Chờ 1 frame nữa để UI hoàn tất layout
        yield return null;

        // 🔻 Tắt shop sau khi setup xong
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerStats != null && coinText != null && gemText != null)
        {
            coinText.text = playerStats.coin.ToString();
            gemText.text = playerStats.gem.ToString();
        }
    }

    void SetupShop()
    {
        // 🧹 Clear toàn bộ slot test còn lại trong editor
        foreach (Transform child in shopPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // 🧱 Sau đó mới tạo slot mới từ prefab
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, shopPanel.transform);

            if (i < shopItems.Count)
            {
                var data = shopItems[i];
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);

                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    Item itemScript = item.GetComponent<Item>();
                    if (itemScript != null)
                    {
                        itemScript.quantity = data.quantity;
                        itemScript.UpdateQuantityDisplay();
                    }

                    slot.GetComponent<Slot>().isShopSlot = true;

                    Slot slotComp = slot.GetComponent<Slot>();
                    if (slotComp != null) slotComp.currentItem = item;

                    var shopHandler = item.AddComponent<ItemShopHandler>();
                    shopHandler.itemID = data.itemID;
                    shopHandler.price = data.price;
                    shopHandler.currency = data.currency;
                    shopHandler.quantity = data.quantity;
                    shopHandler.shopController = this;
                    shopHandler.previewUI = shopItemPreview;
                }
            }
        }
    }

    public void OpenShop()
    {
        PauseController.SetPause(true);
        shopPanel.SetActive(true);
        MenuController.CanOpenMenu = false;

        commonUI.SetUIVisible(false);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        commonUI.SetUIVisible(true);
        gameObject.SetActive(false);
        PauseController.SetPause(false);

        MenuController.CanOpenMenu = true;
    }
    private void ExecuteBuyItem(int itemID, int price, CurrencyType currency, int quantity)
    {
        var player = Object.FindFirstObjectByType<PlayerStats>();
        bool canBuy = (currency == CurrencyType.Coin && player.coin >= price) ||
                      (currency == CurrencyType.Gem && player.gem >= price);

        if (!canBuy)
        {
            ShowNotification("Không đủ tiền! Bạn không thể mua vật phẩm này.");
            return;
        }

        GameObject itemPrefab = itemDictionary.GetItemPrefab(itemID);
        if (itemPrefab == null)
        {
            ShowNotification("Lỗi hệ thống: Không tìm thấy dữ liệu vật phẩm!");
            return;
        }

        Item tempItem = itemPrefab.GetComponent<Item>();
        if (tempItem == null)
        {
            ShowNotification("Lỗi hệ thống: Vật phẩm không có thuộc tính Item!");
            return;
        }

        bool allAdded = true;

        if (tempItem.itemType == ItemType.Equipment)
        {
            for (int i = 0; i < quantity; i++)
            {
                bool added = InventoryController.Instance.AddItem(itemPrefab);
                if (!added)
                {
                    allAdded = false;
                    break;
                }
            }
        }
        else
        {
            GameObject temp = Instantiate(itemPrefab);
            temp.GetComponent<Item>().quantity = quantity;
            allAdded = InventoryController.Instance.AddItem(temp);
            Destroy(temp);
        }

        if (allAdded)
        {
            if (currency == CurrencyType.Coin)
                player.SpendCoin(price);
            else
                player.SpendGem(price);

            if (notifyUIPrefab != null)
            {
                GameObject okUIObj = Instantiate(notifyUIPrefab);
                NotifyUIController notifyUIController = okUIObj.GetComponent<NotifyUIController>();
                notifyUIController.Show("Mua thành công!");
            }
        }
        else
        {
            ShowNotification("Túi đồ đầy! Không thể mua thêm vật phẩm.");
        }

        SaveController saveController = FindFirstObjectByType<SaveController>();
        saveController.SaveGame();
    }
    public void OpenBuyConfirm(int itemID, int price, CurrencyType currency, int quantity)
    {
        if (confirmUIPrefab == null)
        {
            Debug.LogError("ConfirmUIPrefab chưa được load!");
            return;
        }

        GameObject confirmUIObj = Instantiate(confirmUIPrefab);
        ConfirmUIController confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        string itemName = "vật phẩm này";
        if (itemDictionary != null)
        {
            GameObject itemPrefab = itemDictionary.GetItemPrefab(itemID);
            if (itemPrefab != null)
            {
                Item itemInfo = itemPrefab.GetComponent<Item>();
                if (itemInfo != null && !string.IsNullOrEmpty(itemInfo.Name))
                {
                    itemName = $"<color=white>{itemInfo.Name}</color>";
                }
            }
        }

        string currencyText = currency == CurrencyType.Coin ? "coin" : "gem";
        string message = $"Bạn có chắc muốn mua {itemName} với giá {price} {currencyText}?";

        confirmUI.Show(message, () =>
        {
            ExecuteBuyItem(itemID, price, currency, quantity);
        });
    }
    public void ShowNotification(string message)
    {
        if (notifyUIPrefab == null)
        {
            Debug.LogWarning($"NotifyUI Prefab bị thiếu, chỉ hiển thị Debug: {message}");
            return;
        }

        GameObject notifyObj = Instantiate(notifyUIPrefab);
        NotifyUIController notifyController = notifyObj.GetComponent<NotifyUIController>();

        if (notifyController != null)
        {
            notifyController.Show(message);
        }
        else
        {
            Debug.LogError($"NotifyUICanvas thiếu component NotifyUIController. Thông báo: {message}");
            Destroy(notifyObj);
        }
    }
    TextMeshProUGUI FindTextByName(Transform parent, string childName)
    {
        var texts = parent.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
        foreach (var t in texts)
        {
            if (t.name == childName)
                return t;
        }
        return null;
    }
}

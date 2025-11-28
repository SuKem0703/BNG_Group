using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [Header("Cấu hình shop")]
    public GameObject shopPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public List<ShopItemData> shopItems;
    [SerializeField] private ShopItemPreview shopItemPreview;
    private GameObject ConfirmUIPrefab => LoadResourceManager.Instance.ConfirmUIPrefab;
    private GameObject NotifyUIPrefab => LoadResourceManager.Instance.NotifyUIPrefab;

    private ItemDictionary itemDictionary;
    private PlayerStats playerStats;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI gemText;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();

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
    }

    void Start()
    {
        StartCoroutine(SetupAndDeactivate());
    }

    IEnumerator SetupAndDeactivate()
    {
        yield return null;
        SetupShop();
        yield return null;
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
        foreach (Transform child in shopPanel.transform)
        {
            Destroy(child.gameObject);
        }

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
        // Gọi MenuStateManager để xử lý Pause, chặn Input, ẩn HUD
        if (MenuStateManager.Instance != null)
        {
            // Bật gameObject chứa script này lên trước nếu nó đang tắt
            gameObject.SetActive(true);

            // Gọi hàm mở menu chung
            MenuStateManager.Instance.OpenMenu(shopPanel);
        }
        else
        {
            // Fallback nếu không có Manager (ít xảy ra)
            gameObject.SetActive(true);
            shopPanel.SetActive(true);
            PauseController.SetPause(true);
        }
    }

    public void CloseShop()
    {
        // Đóng shop bằng Menu Manager
        if (MenuStateManager.Instance != null)
        {
            MenuStateManager.Instance.CloseCurrentMenu();
        }
        else
        {
            shopPanel.SetActive(false);
            PauseController.SetPause(false);
        }

        gameObject.SetActive(false);
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

        // Trang bị → tạo từng món
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
            // Consumable → tạo 1 item với quantity
            GameObject temp = Instantiate(itemPrefab);
            temp.GetComponent<Item>().quantity = quantity;

            allAdded = InventoryController.Instance.AddItem(temp);

            Destroy(temp);
        }

        if (allAdded)
        {
            // Thanh toán
            if (currency == CurrencyType.Coin)
                player.SpendCoin(price);
            else
                player.SpendGem(price);

            // --- CLEAN VERSION: dùng Prefab Manager ---
            GameObject okUIObj = Instantiate(LoadResourceManager.Instance.NotifyUIPrefab);
            var notify = okUIObj.GetComponent<NotifyUIController>();
            if (notify != null)
                notify.Show("Mua thành công!");

            // Auto Save
            SaveController.Instance?.TriggerAutoSave();
        }
        else
        {
            ShowNotification("Túi đồ đầy! Không thể mua thêm vật phẩm.");
        }
    }


    public void OpenBuyConfirm(int itemID, int price, CurrencyType currency, int quantity)
    {
        if (ConfirmUIPrefab == null)
        {
            Debug.LogError("ConfirmUIPrefab NOT FOUND in LoadResourceManager!");
            return;
        }

        GameObject confirmUIObj = Instantiate(ConfirmUIPrefab);
        var confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        string itemName = "vật phẩm này";

        GameObject pf = itemDictionary.GetItemPrefab(itemID);
        if (pf != null)
        {
            var info = pf.GetComponent<Item>();
            if (info != null && !string.IsNullOrEmpty(info.Name))
                itemName = $"<color=white>{info.Name}</color>";
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
        if (NotifyUIPrefab == null)
        {
            Debug.LogError("NotifyUIPrefab NOT FOUND in LoadResourceManager!");
            return;
        }

        GameObject obj = Instantiate(NotifyUIPrefab);
        var controller = obj.GetComponent<NotifyUIController>();

        if (controller != null)
            controller.Show(message);
        else
        {
            Debug.LogError("NotifyUIPrefab missing NotifyUIController!");
            Destroy(obj);
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
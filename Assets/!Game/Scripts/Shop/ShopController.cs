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
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI gemText;

    private void Awake()
    {
        coinText = FindTextByName(transform, "CoinText");
        gemText = FindTextByName(transform, "GemText");
        itemDictionary = FindFirstObjectByType<ItemDictionary>();
        shopItemPreview = GetComponentsInChildren<ShopItemPreview>(true).FirstOrDefault(x => x.name == "ItemPreview");
    }

    void Start() { StartCoroutine(SetupAndDeactivate()); }
    IEnumerator SetupAndDeactivate() { yield return null; SetupShop(); yield return null; gameObject.SetActive(false); }

    void Update()
    {
        if (PlayerStats.Instance != null && coinText != null && gemText != null)
        {
            coinText.text = PlayerStats.Instance.coin.ToString();
            gemText.text = PlayerStats.Instance.gem.ToString();
        }
    }

    void SetupShop()
    {
        foreach (Transform child in shopPanel.transform) Destroy(child.gameObject);

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
                    if (itemScript != null) { itemScript.quantity = data.quantity; itemScript.UpdateQuantityDisplay(); }

                    slot.GetComponent<Slot>().isShopSlot = true;
                    slot.GetComponent<Slot>().currentItem = item;

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
        if (MenuStateManager.Instance != null) { gameObject.SetActive(true); MenuStateManager.Instance.OpenMenu(shopPanel); }
        else { gameObject.SetActive(true); shopPanel.SetActive(true); PauseController.SetPause(true); }
    }

    public void CloseShop()
    {
        if (MenuStateManager.Instance != null) MenuStateManager.Instance.CloseCurrentMenu();
        else { shopPanel.SetActive(false); PauseController.SetPause(false); }
        gameObject.SetActive(false);
    }

    private void ExecuteBuyItem(int itemID, int price, CurrencyType currency, int quantity)
    {
        bool isCoin = currency == CurrencyType.Coin;
        int currentBalance = isCoin ? PlayerStats.Instance.coin : PlayerStats.Instance.gem;

        if (currentBalance < price)
        {
            ShowNotification("Số dư không đủ!");
            return;
        }

        bool isStackable = true;

        GameObject itemPrefab = itemDictionary.GetItemPrefab(itemID);
        if (itemPrefab != null)
        {
            Item itemScript = itemPrefab.GetComponent<Item>();
            if (itemScript != null)
            {
                isStackable = itemScript.IsStackable;
            }
        }

        string currencyStr = isCoin ? "Coin" : "Gem";

        InventoryService.Instance.RequestBuyItem(itemID, quantity, price, currencyStr, isStackable, (success, serverItems) =>
        {
            if (success)
            {
                ShowNotification("Mua thành công!");

                int newBalance = currentBalance - price;
                if (isCoin) PlayerStats.Instance.SyncCoinFromServer(newBalance);
                else PlayerStats.Instance.SyncGemFromServer(newBalance);

                if (InventoryController.Instance != null) InventoryController.Instance.RefreshInventory();
            }
            else
            {
                ShowNotification("Giao dịch thất bại!");
            }
        });
    }

    public void OpenBuyConfirm(int itemID, int price, CurrencyType currency, int quantity)
    {
        if (ConfirmUIPrefab == null) return;
        GameObject confirmUIObj = Instantiate(ConfirmUIPrefab);
        var confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        string itemName = "vật phẩm này";
        GameObject pf = itemDictionary.GetItemPrefab(itemID);
        if (pf != null) itemName = $"<color=white>{pf.GetComponent<Item>().Name}</color>";
        string curStr = currency == CurrencyType.Coin ? "coin" : "gem";

        confirmUI.Show($"Bạn có chắc muốn mua {itemName} với giá {price} {curStr}?", () =>
        {
            ExecuteBuyItem(itemID, price, currency, quantity);
        });
    }

    public void ShowNotification(string message)
    {
        if (NotifyUIPrefab == null) return;
        Instantiate(NotifyUIPrefab).GetComponent<NotifyUIController>()?.Show(message);
    }

    TextMeshProUGUI FindTextByName(Transform p, string n)
    {
        foreach (var t in p.GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == n) return t;
        return null;
    }
}
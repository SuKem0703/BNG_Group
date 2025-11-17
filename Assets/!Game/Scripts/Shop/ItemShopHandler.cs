using UnityEngine;
using UnityEngine.EventSystems;

public class ItemShopHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public int itemID;
    public int price;
    public CurrencyType currency;
    public int quantity = 1;
    public ShopController shopController;
    public ShopItemPreview previewUI;
    public void OnPointerClick(PointerEventData eventData)
    {
        shopController.OpenBuyConfirm(itemID, price, currency, quantity);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Item item = GetComponent<Item>();
        if (item != null && previewUI != null)
            previewUI.Show(item, price, currency);
    }
}

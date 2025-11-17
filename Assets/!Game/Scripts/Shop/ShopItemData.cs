using UnityEngine;

[System.Serializable]
public class ShopItemData
{
    public int itemID;
    public int price;
    public CurrencyType currency;
    public int quantity = 1; // ← số lượng item hiển thị trong shop
}

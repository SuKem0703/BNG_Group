using TMPro;
using UnityEngine;

public enum ItemType { QuestItem, Equipment, Consumable, Seed, Material }
public enum CurrencyType { Coin, Gem }
public enum EquipSlot { None, Swords, Shield, Helmet, Armor, Scepter, Amulet, Hat, Robe, Gloves, Legs, Belt, Boots, Ring, Necklace }
public enum ClassRestriction { None, Knight, Mage }
public enum ItemRarity { Rusty, Common, Refined, Rare, Relic, Glacial, Legendary, Celestial, Mythic }

public abstract class Item : MonoBehaviour
{
    [Header("Trạng thái cơ bản")]
    public bool isDisplayOnly = false;
    public Item sourceItem;

    [Header("Thông tin định danh")]
    public int dbID = 0;
    public int ID;
    public string Name;
    [TextArea] public string description;
    public Sprite icon;

    public int quantity = 1;

    [Range(0.5f, 1.0f)]
    public float qualityFactor = 1f;
    public ItemRarity rarity = ItemRarity.Common;

    [SerializeField] protected TMP_Text quantityTextOnUI;
    [SerializeField] protected TMP_Text quantityTextOnWorld;

    // Các thuộc tính trừu tượng bắt buộc lớp con phải khai báo
    public abstract ItemType ItemType { get; }
    public abstract bool IsStackable { get; }

    protected virtual void Awake()
    {
        if (quantityTextOnUI == null || quantityTextOnWorld == null)
        {
            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in allTexts)
            {
                if (text.name == "QuantityTextOnUI") quantityTextOnUI = text;
                else if (text.name == "QuantityTextOnWorld") quantityTextOnWorld = text;
            }
        }
        UpdateQuantityDisplay();
    }

    public void UpdateQuantityDisplay()
    {
        string displayText = (IsStackable && quantity > 1) ? quantity.ToString() : "";

        if (quantityTextOnUI != null) quantityTextOnUI.text = displayText;
        if (quantityTextOnWorld != null) quantityTextOnWorld.text = displayText;
    }

    protected float GetFinalMultiplier()
    {
        // return ItemRarityMultiplier.GetMultiplier(rarity) * qualityFactor;
        return 1f * qualityFactor;
    }

    public void AddToStack(int amount = 1)
    {
        if (!IsStackable) return;
        quantity += amount;
        UpdateQuantityDisplay();
    }

    public int RemoveFromStack(int amount = 1)
    {
        if (!IsStackable) return 0;
        int removed = Mathf.Min(amount, quantity);
        quantity -= removed;
        UpdateQuantityDisplay();
        return removed;
    }

    public GameObject CloneItem(int newQuantity)
    {
        GameObject clone = Instantiate(gameObject);
        Item cloneItem = clone.GetComponent<Item>();
        cloneItem.quantity = newQuantity;
        cloneItem.UpdateQuantityDisplay();
        return clone;
    }

    // Bắt buộc lớp con tự định nghĩa logic sử dụng
    public abstract void UseItem();

    public virtual void ShowPopUp()
    {
        if (ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPickup(Name, icon, rarity);
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || icon == null) return;

            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != icon)
            {
                sr.sprite = icon;
                UnityEditor.EditorUtility.SetDirty(sr);
            }

            var uiImage = GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (uiImage != null && uiImage.sprite != icon)
            {
                uiImage.sprite = icon;
                UnityEditor.EditorUtility.SetDirty(uiImage);
            }
        };
    }

    [ContextMenu("Sync ID from Name")]
    public void SyncIDWithFileName()
    {
        if (!UnityEditor.AssetDatabase.Contains(gameObject)) return;

        string path = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

        if (int.TryParse(fileName, out int newID))
        {
            if (ID != newID)
            {
                UnityEditor.Undo.RecordObject(this, "Sync ID from Name");
                ID = newID;
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"<color=green>[Sync]</color> Đã cập nhật ID thành: {ID}");
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[Sync]</color> Tên file '{fileName}' không phải là số hợp lệ để làm ID.");
        }
    }

    [ContextMenu("Sync Name with ID")]
    public void SyncPrefabNameWithID()
    {
        if (!UnityEditor.AssetDatabase.Contains(gameObject)) return;
        if (ID == 0) return;

        string path = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
        string currentName = System.IO.Path.GetFileNameWithoutExtension(path);
        string newName = ID.ToString();

        if (currentName != newName)
        {
            string result = UnityEditor.AssetDatabase.RenameAsset(path, newName);
            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"<color=cyan>[Sync]</color> Đã đổi tên Prefab thành: {newName}");
            }
            else
            {
                Debug.LogError($"Lỗi đổi tên: {result}");
            }
        }
    }
#endif
}
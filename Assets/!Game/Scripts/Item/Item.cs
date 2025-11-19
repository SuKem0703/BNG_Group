using TMPro;
using UnityEngine;

public enum ItemType
{
    QuestItem,
    Equipment,
    Consumable,
    Seed,
    Material,
}
public enum CurrencyType
{
    Coin, Gem
}
public enum EquipSlot
{
    None,

    Swords,
    Shield,
    Helmet,
    Armor,

    Staff,
    Catalyst,
    Hat,
    Robe,

    Gloves,
    Legs,
    Belt,
    Boots,
    Ring,
    Necklace
}
public enum ClassRestriction
{
    None,
    Knight,
    Mage
}
public enum ItemRarity
{
    Rusty,
    Common,
    Refined,
    Rare,
    Relic,
    Glacial,
    Legendary,
    Celestial,
    Mythic
}

public class Item : MonoBehaviour
{
    public bool isDisplayOnly = false;
    public bool isEquipped = false;
    public Item sourceItem;

    [Header("Thông tin cơ bản")]
    public int ID;
    public string Name;
    [TextArea]
    public string description;
    public Sprite icon;

    public int quantity = 1;
    public ItemType itemType;

    [Header("Tiềm năng gốc")]
    [SerializeField] private int strBase;
    [SerializeField] private int dexBase;
    [SerializeField] private int conBase;
    [SerializeField] private int intBase;

    [Header("Yêu cầu")]
    public int requiredLevel;

    [Header("Chỉ số cộng thêm gốc")]
    [SerializeField] private int physDamageBase;
    [SerializeField] private int magicDamageBase;
    [SerializeField] private int defenseBase;
    [SerializeField] private int hpKnightBase;
    [SerializeField] private int hpMageBase;
    [SerializeField] private int mpKnightBase;
    [SerializeField] private int mpMageBase;
    [SerializeField] private int hpRegenBase;
    [SerializeField] private int mpRegenBase;
    [SerializeField] private float critRateBase;
    [SerializeField] private float moveSpeedBase;
    [SerializeField] private int staminaRegenBase;

    public float damageReduction;

    [Range(0.5f, 1.0f)]
    public float qualityFactor = 1f;

    [Header("Trang bị")]
    public EquipSlot equipSlot = EquipSlot.None;
    public ClassRestriction classRestriction = ClassRestriction.None;
    public ItemRarity rarity = ItemRarity.Common;

    [SerializeField] private TMP_Text quantityTextOnUI;
    [SerializeField] private TMP_Text quantityTextOnWorld;
    protected virtual void Awake()
    {
        if (quantityTextOnUI == null || quantityTextOnWorld == null)
        {
            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in allTexts)
            {
                if (text.name == "QuantityTextOnUI")
                {
                    quantityTextOnUI = text;
                }
                else if (text.name == "QuantityTextOnWorld")
                {
                    quantityTextOnWorld = text;
                }
            }
        }

        UpdateQuantityDisplay();
    }
    public void UpdateQuantityDisplay()
    {
        string displayText = (itemType != ItemType.Equipment && quantity > 1) ? quantity.ToString() : "";

        if (quantityTextOnUI != null)
        {
            quantityTextOnUI.text = displayText;
        }
        if (quantityTextOnWorld != null)
        {
            quantityTextOnWorld.text = displayText;
        }
    }
    private float GetFinalMultiplier()
    {
        return ItemRarityMultiplier.GetMultiplier(rarity) * qualityFactor;
    }

    public int bonusSTR => Mathf.RoundToInt(strBase * GetFinalMultiplier());
    public int bonusDEX => Mathf.RoundToInt(dexBase * GetFinalMultiplier());
    public int bonusCON => Mathf.RoundToInt(conBase * GetFinalMultiplier());
    public int bonusINT => Mathf.RoundToInt(intBase * GetFinalMultiplier());
    public int physDamageBonus => Mathf.RoundToInt(physDamageBase * GetFinalMultiplier());
    public int magicDamageBonus => Mathf.RoundToInt(magicDamageBase * GetFinalMultiplier());
    public int defenseBonus => Mathf.RoundToInt(defenseBase * GetFinalMultiplier());
    public int hpKnightBonus => Mathf.RoundToInt(hpKnightBase * GetFinalMultiplier());
    public int hpMageBonus => Mathf.RoundToInt(hpMageBase * GetFinalMultiplier());
    public int mpKnightBonus => Mathf.RoundToInt(mpKnightBase * GetFinalMultiplier());
    public int mpMageBonus => Mathf.RoundToInt(mpMageBase * GetFinalMultiplier());
    public int hpRegenBonus => Mathf.RoundToInt(hpRegenBase * GetFinalMultiplier());
    public int mpRegenBonus => Mathf.RoundToInt(mpRegenBase * GetFinalMultiplier());
    public float critRateBonus => critRateBase * GetFinalMultiplier();
    public float moveSpeedBonus => moveSpeedBase * GetFinalMultiplier();
    public int staminaRegenBonus => Mathf.RoundToInt(staminaRegenBase * GetFinalMultiplier());

    public void AddToStack(int amount = 1)
    {
        if (itemType == ItemType.Equipment) return;
        quantity += amount;
        UpdateQuantityDisplay();
    }

    public int RemoveFromStack(int amount = 1)
    {
        if (itemType == ItemType.Equipment) return 0;
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

    public virtual void UseItem()
    {
        Debug.Log("Dùng item: " + Name);
    }

    public virtual void ShowPopUp()
    {
        if (ItemPickupUIController.Instance != null)
        {
            ItemPickupUIController.Instance.ShowItemPickup(Name, icon, rarity);
        }
    }
}

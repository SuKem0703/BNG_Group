using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemPreview : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image itemPortrait;
    [SerializeField] private TMP_Text lvl;
    [SerializeField] private TMP_Text classText;
    [SerializeField] private TMP_Text slotText;

    [Header("Stat Fields")]
    [SerializeField] private TMP_Text statName1;
    [SerializeField] private TMP_Text statValue1;
    [SerializeField] private TMP_Text statName2;
    [SerializeField] private TMP_Text statValue2;
    [SerializeField] private TMP_Text statName3;
    [SerializeField] private TMP_Text statValue3;

    [Header("Price Display")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image currencyImage;

    [Header("Currency Sprites (Set trong Inspector)")]
    public Sprite coinSprite;
    public Sprite gemSprite;

    void Awake()
    {
        Transform t = transform;
        nameText = t.FindDeepChild("NameText").GetComponent<TMP_Text>();
        itemPortrait = t.FindDeepChild("ItemPortrait").GetComponent<Image>();
        lvl = t.FindDeepChild("LevelText").GetComponent<TMP_Text>();
        classText = t.FindDeepChild("ClassText").GetComponent<TMP_Text>();
        slotText = t.FindDeepChild("SlotText").GetComponent<TMP_Text>();

        statName1 = t.FindDeepChild("statName1").GetComponent<TMP_Text>();
        statValue1 = t.FindDeepChild("statValue1").GetComponent<TMP_Text>();
        statName2 = t.FindDeepChild("statName2").GetComponent<TMP_Text>();
        statValue2 = t.FindDeepChild("statValue2").GetComponent<TMP_Text>();
        statName3 = t.FindDeepChild("statName3").GetComponent<TMP_Text>();
        statValue3 = t.FindDeepChild("statValue3").GetComponent<TMP_Text>();

        priceText = t.FindDeepChild("PriceText").GetComponent<TMP_Text>();
        currencyImage = t.FindDeepChild("CurrencyImage").GetComponent<Image>();

        Hide();
    }

    public void Show(Item item, int price, CurrencyType currency)
    {
        if (item == null) return;
        gameObject.SetActive(true);

        nameText.text = item.Name;
        itemPortrait.sprite = item.icon;
        lvl.text = "Lvl: " + item.requiredLevel;
        classText.text = "Class: " + item.classRestriction;
        slotText.text = "Loại: " + item.equipSlot;

        // Cập nhật 3 chỉ số giống EquipTooltip
        string[] stats = GetStatsFor(item);
        FillStatLine(statName1, statValue1, stats[0], item);
        FillStatLine(statName2, statValue2, stats[1], item);
        FillStatLine(statName3, statValue3, stats[2], item);

        // Hiển thị giá và icon
        priceText.text = $"Giá: {price}";
        currencyImage.sprite = (currency == CurrencyType.Coin ? coinSprite : gemSprite);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void FillStatLine(TMP_Text nameField, TMP_Text valueField, string statType, Item item)
    {
        if (string.IsNullOrEmpty(statType))
        {
            nameField.text = "";
            valueField.text = "";
            return;
        }

        switch (statType)
        {
            case "PhysDmg":
                nameField.text = "PHYSICAL DMG:";
                valueField.text = item.physDamageBonus.ToString();
                break;

            case "MagicDmg":
                nameField.text = "MAGIC DMG:";
                valueField.text = item.magicDamageBonus.ToString();
                break;

            case "DEF":
                nameField.text = "DEF:";
                valueField.text = item.defenseBonus.ToString();
                break;

            case "HP":
                nameField.text = "HP:";
                valueField.text = (item.classRestriction == ClassRestriction.Knight
                    ? item.hpKnightBonus
                    : item.hpMageBonus).ToString();
                break;

            case "MP":
                nameField.text = "MP:";
                valueField.text = (item.classRestriction == ClassRestriction.Knight
                    ? item.mpKnightBonus
                    : item.mpMageBonus).ToString();
                break;

            case "STR":
                nameField.text = "STR:";
                valueField.text = item.bonusSTR.ToString();
                break;

            case "DEX":
                nameField.text = "DEX:";
                valueField.text = item.bonusDEX.ToString();
                break;

            case "CON":
                nameField.text = "CON:";
                valueField.text = item.bonusCON.ToString();
                break;

            case "INT":
                nameField.text = "INT:";
                valueField.text = item.bonusINT.ToString();
                break;

            case "HPRegen":
                nameField.text = "HP REGEN:";
                valueField.text = item.hpRegenBonus.ToString();
                break;

            case "MPRegen":
                nameField.text = "MP REGEN:";
                valueField.text = item.mpRegenBonus.ToString();
                break;

            case "CritRate":
                nameField.text = "CRIT RATE:";
                valueField.text = item.critRateBonus.ToString("F1") + "%";
                break;

            case "DmgReduction":
                nameField.text = "DMG REDUCTION:";
                valueField.text = (item.damageReduction * 100f).ToString("F1") + "%";
                break;

            default:
                nameField.text = "";
                valueField.text = "";
                break;
        }
    }

    private string[] GetStatsFor(Item item)
    {
        if (item.classRestriction == ClassRestriction.Knight)
        {
            switch (item.equipSlot)
            {
                case EquipSlot.Swords: return new[] { "PhysDmg", "HP", "STR" };
                case EquipSlot.Shield: return new[] { "DEF", "HP", "CON" };
                case EquipSlot.Helmet: return new[] { "DEF", "CON", "HP" };
                case EquipSlot.Armor: return new[] { "DEF", "STR", "HP" };
            }
        }
        else if (item.classRestriction == ClassRestriction.Mage)
        {
            switch (item.equipSlot)
            {
                case EquipSlot.Staff: return new[] { "MagicDmg", "MP", "INT" };
                case EquipSlot.Catalyst: return new[] { "MagicDmg", "CritRate", "INT" };
                case EquipSlot.Hat: return new[] { "DEF", "HP", "INT" };
                case EquipSlot.Robe: return new[] { "DEF", "MP", "INT" };
            }
        }
        return new string[] { "PhysDmg", "HP", "STR" }; // fallback
    }
}

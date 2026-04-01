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
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TMP_Text statName1;
    [SerializeField] private TMP_Text statValue1;
    [SerializeField] private TMP_Text statName2;
    [SerializeField] private TMP_Text statValue2;
    [SerializeField] private TMP_Text statName3;
    [SerializeField] private TMP_Text statValue3;

    [Header("Price Display")]
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image currencyImage;

    public Sprite coinSprite;
    public Sprite gemSprite;

    public void Show(Item item, int price, CurrencyType currency)
    {
        if (item == null) return;
        gameObject.SetActive(true);

        nameText.text = item.Name;
        itemPortrait.sprite = item.icon;

        // Hiển thị giá
        priceText.text = $"Giá: {price}";
        currencyImage.sprite = (currency == CurrencyType.Coin ? coinSprite : gemSprite);

        // Kiểm tra nếu là Trang bị thì mới hiện chỉ số
        if (item is EquipmentItem equip)
        {
            if (statsPanel != null) statsPanel.SetActive(true);

            lvl.text = "Lvl: " + equip.requiredLevel;
            classText.text = "Class: " + equip.classRestriction;
            slotText.text = "Loại: " + equip.equipSlot;

            string[] stats = GetStatsFor(equip);
            FillStatLine(statName1, statValue1, stats[0], equip);
            FillStatLine(statName2, statValue2, stats[1], equip);
            FillStatLine(statName3, statValue3, stats[2], equip);
        }
        else
        {
            // Nếu là vật phẩm thường, ẩn bảng chỉ số
            if (statsPanel != null) statsPanel.SetActive(false);
            lvl.text = "";
            classText.text = "Phân loại: " + item.ItemType;
            slotText.text = "";
        }
    }

    private void FillStatLine(TMP_Text nameField, TMP_Text valueField, string statType, EquipmentItem item)
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
                valueField.text = (item.classRestriction == ClassRestriction.Knight ? item.hpKnightBonus : item.hpMageBonus).ToString();
                break;
            case "MP":
                nameField.text = "MP:";
                valueField.text = (item.classRestriction == ClassRestriction.Knight ? item.mpKnightBonus : item.mpMageBonus).ToString();
                break;
            case "STR": nameField.text = "STR:"; valueField.text = item.bonusSTR.ToString(); break;
            case "DEX": nameField.text = "DEX:"; valueField.text = item.bonusDEX.ToString(); break;
            case "CON": nameField.text = "CON:"; valueField.text = item.bonusCON.ToString(); break;
            case "INT": nameField.text = "INT:"; valueField.text = item.bonusINT.ToString(); break;
            case "CritRate":
                nameField.text = "CRIT RATE:";
                valueField.text = item.critRateBonus.ToString("F1") + "%";
                break;
            default:
                nameField.text = "";
                valueField.text = "";
                break;
        }
    }

    private string[] GetStatsFor(EquipmentItem item)
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
                case EquipSlot.Scepter: return new[] { "MagicDmg", "MP", "INT" };
                case EquipSlot.Amulet: return new[] { "MagicDmg", "CritRate", "INT" };
                case EquipSlot.Hat: return new[] { "DEF", "HP", "INT" };
                case EquipSlot.Robe: return new[] { "DEF", "MP", "INT" };
            }
        }
        return new string[] { null, null, null };
    }

    public void Hide() => gameObject.SetActive(false);
}
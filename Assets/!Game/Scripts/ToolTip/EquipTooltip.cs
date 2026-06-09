using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipTooltip : MonoBehaviour
{
    public static EquipTooltip Instance;

    [Header("Cấu trúc UI")]
    [SerializeField] private Image backGround;
    [SerializeField] private Image borderFrame;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image itemPortrait;
    [SerializeField] private TMP_Text slotText;
    [SerializeField] private TMP_Text lvl;
    [SerializeField] private TMP_Text classText;
    [SerializeField] private Image isEquip;

    [Header("Chỉ số hiển thị")]
    [SerializeField] private TMP_Text statName1;
    [SerializeField] private TMP_Text statValue1;
    [SerializeField] private TMP_Text statName2;
    [SerializeField] private TMP_Text statValue2;
    [SerializeField] private TMP_Text statName3;
    [SerializeField] private TMP_Text statValue3;

    [SerializeField] private TMP_Text descriptionText;

    void Awake()
    {
        Instance = this;
        
        Transform t = transform;

        backGround = t.Find("backGround").GetComponent<Image>();
        borderFrame = t.Find("borderFrame").GetComponent<Image>();
        itemPortrait = t.Find("itemPortrait").GetComponent<Image>();
        nameText = t.Find("nameText").GetComponent<TextMeshProUGUI>();
        slotText = t.Find("slotText").GetComponent<TextMeshProUGUI>();
        lvl = t.Find("lvl").GetComponent<TextMeshProUGUI>();
        classText = t.Find("classText").GetComponent<TextMeshProUGUI>();

        statValue1 = t.FindDeepChild("statValue1").GetComponent<TextMeshProUGUI>();
        statName1 = t.FindDeepChild("statName1").GetComponent<TextMeshProUGUI>();
        statValue2 = t.FindDeepChild("statValue2").GetComponent<TextMeshProUGUI>();
        statName2 = t.FindDeepChild("statName2").GetComponent<TextMeshProUGUI>();
        statValue3 = t.FindDeepChild("statValue3").GetComponent<TextMeshProUGUI>();
        statName3 = t.FindDeepChild("statName3").GetComponent<TextMeshProUGUI>();

        descriptionText = t.Find("descriptionText").GetComponent<TextMeshProUGUI>();
        isEquip = t.Find("isEquip").GetComponent<Image>();

        gameObject.SetActive(false);
    }
    void Update()
    {
        if (!gameObject.activeSelf) return;

        RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        RectTransform tooltipRect = gameObject.GetComponent<RectTransform>();

        tooltipRect.pivot = new Vector2(1f, 0f);

        Vector2 mousePos = Input.mousePosition;

        Vector2 offset = new Vector2(0f, 0f);
        mousePos += offset;

        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out anchoredPos);

        float tooltipWidth = 600f * 0.6f;
        float tooltipHeight = 800f * 0.6f;

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float minX = -canvasWidth / 2f + tooltipWidth;
        float maxX = canvasWidth / 2f;
        float minY = -canvasHeight / 2f;
        float maxY = canvasHeight / 2f - tooltipHeight;

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        tooltipRect.anchoredPosition = anchoredPos;
    }

    public void Show(Item item, Slot slot)
    {
        if (item == null) return;
        if (item is not EquipmentItem equipItem) return;
        gameObject.SetActive(true);

        ItemRarity rarity = equipItem.rarity;
        string path = $"Vertical Card/{rarity}";
        Sprite raritySprite = Resources.Load<Sprite>(path);
        if (raritySprite != null)
            borderFrame.sprite = raritySprite;
        else
            Debug.LogWarning($"Không tìm thấy sprite cho rarity '{rarity}' tại: {path}");

        nameText.text = equipItem.Name;
        backGround.color = RarityColorHelper.GetColorByRarity(equipItem.rarity);
        itemPortrait.sprite = equipItem.icon;
        slotText.text = "Loại: " + equipItem.equipSlot;
        lvl.text = "Lvl: " + equipItem.requiredLevel;
        classText.text = "Class: " + equipItem.classRestriction;
        descriptionText.text = equipItem.description;

        if (isEquip != null)
        {
            if (item.isDisplayOnly == true && equipItem.isEquipped == false || slot.isShopSlot == true)
            {
                string dotSpriteName = "";

                PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();

                if (equipItem.requiredLevel <= playerStats.level)
                {
                    dotSpriteName = "IconFlag/Green_Dot";
                }
                else
                {
                    dotSpriteName = "IconFlag/Red_Dot";
                }

                Sprite dotSprite = Resources.Load<Sprite>(dotSpriteName);

                if (dotSprite != null)
                {
                    isEquip.sprite = dotSprite;
                    isEquip.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogError($"Thiếu Sprite cờ báo hiệu: {dotSpriteName}");
                    isEquip.gameObject.SetActive(false);
                }
            }
            else
            {
                string togglePath = "IconFlag/";
                string spriteName = equipItem.isEquipped ? "Toggle_ON" : "Toggle_OFF";
                Sprite equipSprite = Resources.Load<Sprite>(togglePath + spriteName);

                if (equipSprite != null)
                {
                    isEquip.sprite = equipSprite;
                    isEquip.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogError($"Thiếu Sprite cho trạng thái trang bị: {togglePath + spriteName}");
                    isEquip.gameObject.SetActive(false);
                }
            }
        }

        string[] stats = GetStatsFor(item);
        FillStatLine(statName1, statValue1, stats[0], item);
        FillStatLine(statName2, statValue2, stats[1], item);
        FillStatLine(statName3, statValue3, stats[2], item);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void FillStatLine(TMP_Text nameField, TMP_Text valueField, string statType, Item item)
    {
        if (item is not EquipmentItem equipItem) return;

        if (statType == null)
        {
            nameField.text = "";
            valueField.text = "";
            return;
        }

        switch (statType)
        {
            case "PhysDmg":
                nameField.text = "PHYSICAL DMG:";
                valueField.text = equipItem.physDamageBonus.ToString();
                break;

            case "MagicDmg":
                nameField.text = "MAGIC DMG:";
                valueField.text = equipItem.magicDamageBonus.ToString();
                break;

            case "DEF":
                nameField.text = "DEF:";
                valueField.text = equipItem.defenseBonus.ToString();
                break;

            case "HP":
                nameField.text = "HP:";
                valueField.text = (equipItem.classRestriction == ClassRestriction.Knight
                    ? equipItem.hpKnightBonus
                    : equipItem.hpMageBonus).ToString();
                break;

            case "MP":
                nameField.text = "MP:";
                valueField.text = (equipItem.classRestriction == ClassRestriction.Knight
                    ? equipItem.mpKnightBonus
                    : equipItem.mpMageBonus).ToString();
                break;

            case "STR":
                nameField.text = "STR:";
                valueField.text = equipItem.bonusSTR.ToString();
                break;

            case "DEX":
                nameField.text = "DEX:";
                valueField.text = equipItem.bonusDEX.ToString();
                break;

            case "CON":
                nameField.text = "CON:";
                valueField.text = equipItem.bonusCON.ToString();
                break;

            case "INT":
                nameField.text = "INT:";
                valueField.text = equipItem.bonusINT.ToString();
                break;

            case "HPRegen":
                nameField.text = "HP REGEN:";
                valueField.text = equipItem.hpRegenBonus.ToString();
                break;

            case "MPRegen":
                nameField.text = "MP REGEN:";
                valueField.text = equipItem.mpRegenBonus.ToString();
                break;

            case "CritRate":
                nameField.text = "CRIT RATE:";
                valueField.text = equipItem.critRateBonus.ToString("F1") + "%";
                break;

            case "DmgReduction":
                nameField.text = "DMG REDUCTION:";
                valueField.text = (equipItem.damageReduction * 100f).ToString("F1") + "%";
                break;

            default:
                nameField.text = "";
                valueField.text = "";
                break;
        }
    }

    private string[] GetStatsFor(Item item)
    {
        if (item is not EquipmentItem equip)
        {
            return new string[] { null, null, null };
        }

        if (equip.classRestriction == ClassRestriction.Knight)
        {
            switch (equip.equipSlot)
            {
                case EquipSlot.MainHand: return new[] { "PhysDmg", "HP", "STR" };
                case EquipSlot.OffHand: return new[] { "DEF", "DEX", "HPRegen" };
                case EquipSlot.Helmet: return new[] { "DEF", "CON", "HP" };
                case EquipSlot.Armor: return new[] { "DEF", "DEX", "DmgReduction" };
            }
        }
        else if (equip.classRestriction == ClassRestriction.Mage)
        {
            switch (equip.equipSlot)
            {
                case EquipSlot.MainHand: return new[] { "MagicDmg", "MP", "INT" };
                case EquipSlot.OffHand: return new[] { "CritRate", "CON", "MPRegen" };
                case EquipSlot.Hat: return new[] { "DEF", "DEX", "HP" };
                case EquipSlot.Robe: return new[] { "CON", "HPRegen", "DmgReduction" };
            }
        }

        return new string[] { null, null, null };
    }
}
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EquipmentDataRow
{
    public int itemId;
    public string itemName;
    public int reqLevel;

    [Header("Phân loại")]
    public ClassRestriction classRestriction;
    public EquipSlot equipSlot;

    [Header("Hướng dẫn nhập Giá trị (Tự động cập nhật)")]
    [TextArea(1, 2)]
    public string statGuide = "Chọn Class và Slot để xem hướng dẫn nhập...";

    [Header("Điền 3 Giá trị")]
    public float value1;
    public float value2;
    public float value3;
}

[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Game/Equipment Database")]
public class EquipmentDatabase : ScriptableObject
{
    public List<EquipmentDataRow> dataRows = new List<EquipmentDataRow>();

    // Hàm này chạy liên tục trên Inspector, giúp tự động hiển thị Text nhắc nhở
    private void OnValidate()
    {
        foreach (var row in dataRows)
        {
            if (row == null) continue;

            if (row.classRestriction == ClassRestriction.Knight)
            {
                switch (row.equipSlot)
                {
                    case EquipSlot.Swords: row.statGuide = "1: PhysDmg | 2: HP (Knight) | 3: STR"; break;
                    case EquipSlot.Shield: row.statGuide = "1: DEF | 2: DEX | 3: HP Regen"; break;
                    case EquipSlot.Helmet: row.statGuide = "1: DEF | 2: CON | 3: HP (Knight)"; break;
                    case EquipSlot.Armor: row.statGuide = "1: DEF | 2: DEX | 3: Dmg Reduction (%)"; break;
                    default: row.statGuide = "Chưa có cấu hình!"; break;
                }
            }
            else if (row.classRestriction == ClassRestriction.Mage)
            {
                switch (row.equipSlot)
                {
                    case EquipSlot.Scepter: row.statGuide = "1: MagicDmg | 2: MP (Mage) | 3: INT"; break;
                    case EquipSlot.Amulet: row.statGuide = "1: CritRate (%) | 2: CON | 3: MP Regen"; break;
                    case EquipSlot.Hat: row.statGuide = "1: DEF | 2: DEX | 3: HP (Mage)"; break;
                    case EquipSlot.Robe: row.statGuide = "1: CON | 2: HP Regen | 3: Dmg Reduction (%)"; break;
                    default: row.statGuide = "Chưa có cấu hình!"; break;
                }
            }
            else
            {
                row.statGuide = "Chọn Class: Knight hoặc Mage trước.";
            }
        }
    }
}
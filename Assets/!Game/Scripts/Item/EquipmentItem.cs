//using UnityEngine;

//public class EquipmentItem : Item
//{
//    [Header("Trang bị Settings")]
//    public EquipSlot equipSlot = EquipSlot.None;
//    public ClassRestriction classRestriction = ClassRestriction.None;
//    public bool isEquipped = false;
//    public int requiredLevel;

//    [Range(0.5f, 1.0f)]
//    public float qualityFactor = 1f;

//    [Header("Tiềm năng gốc")]
//    [SerializeField] private int strBase;
//    [SerializeField] private int dexBase;
//    [SerializeField] private int conBase;
//    [SerializeField] private int intBase;

//    [Header("Chỉ số cộng thêm gốc")]
//    [SerializeField] private int physDamageBase;
//    [SerializeField] private int magicDamageBase;
//    [SerializeField] private int defenseBase;
//    [SerializeField] private int hpKnightBase;
//    [SerializeField] private int hpMageBase;
//    [SerializeField] private int mpKnightBase;
//    [SerializeField] private int mpMageBase;
//    [SerializeField] private int hpRegenBase;
//    [SerializeField] private int mpRegenBase;
//    [SerializeField] private float critRateBase;
//    [SerializeField] private float moveSpeedBase;
//    [SerializeField] private int staminaRegenBase;

//    public float damageReduction;

//    private void OnValidate()
//    {
//        itemType = ItemType.Equipment;
//    }

//    public override bool IsStackable => false;

//    // Logic tính toán chỉ số
//    private float GetFinalMultiplier()
//    {
//        return ItemRarityMultiplier.GetMultiplier(rarity) * qualityFactor;
//    }

//    // Các Property tính toán chỉ số cuối cùng
//    public int bonusSTR => Mathf.RoundToInt(strBase * GetFinalMultiplier());
//    public int bonusDEX => Mathf.RoundToInt(dexBase * GetFinalMultiplier());
//    public int bonusCON => Mathf.RoundToInt(conBase * GetFinalMultiplier());
//    public int bonusINT => Mathf.RoundToInt(intBase * GetFinalMultiplier());

//    public int physDamageBonus => Mathf.RoundToInt(physDamageBase * GetFinalMultiplier());
//    public int magicDamageBonus => Mathf.RoundToInt(magicDamageBase * GetFinalMultiplier());
//    public int defenseBonus => Mathf.RoundToInt(defenseBase * GetFinalMultiplier());

//    public int hpKnightBonus => Mathf.RoundToInt(hpKnightBase * GetFinalMultiplier());
//    public int hpMageBonus => Mathf.RoundToInt(hpMageBase * GetFinalMultiplier());
//    public int mpKnightBonus => Mathf.RoundToInt(mpKnightBase * GetFinalMultiplier());
//    public int mpMageBonus => Mathf.RoundToInt(mpMageBase * GetFinalMultiplier());

//    public int hpRegenBonus => Mathf.RoundToInt(hpRegenBase * GetFinalMultiplier());
//    public int mpRegenBonus => Mathf.RoundToInt(mpRegenBase * GetFinalMultiplier());

//    public float critRateBonus => critRateBase * GetFinalMultiplier();
//    public float moveSpeedBonus => moveSpeedBase * GetFinalMultiplier();
//    public int staminaRegenBonus => Mathf.RoundToInt(staminaRegenBase * GetFinalMultiplier());

//    public override void UseItem()
//    {
//        Debug.Log($"Trang bị {Name} vào slot {equipSlot}");
//    }
//}
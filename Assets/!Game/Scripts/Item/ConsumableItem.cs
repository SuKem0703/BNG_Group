using UnityEngine;

public class ConsumableItem : Item
{
    public override ItemType ItemType => ItemType.Consumable;
    public override bool IsStackable => true;

    [Header("Consumable Effect")]
    [Tooltip("ID phải khớp với Effect.cs, ví dụ: 'HEAL_INSTANT'")]
    public string effectID;

    [Tooltip("Giá trị của hiệu ứng, ví dụ: 50 (hồi 50 HP)")]
    public float effectValue;

    [Tooltip("Thời gian hiệu ứng. Để 0 nếu là 'tức thì' (instant)")]
    public float effectDuration = 0f;

    [Header("Cooldown")]
    [Tooltip("Đánh dấu true nếu item này kích hoạt và bị ảnh hưởng bởi cooldown Potion toàn cục")]
    public bool triggersGlobalPotionCooldown = false;

    private PlayerStats playerStats;

    protected override void Awake()
    {
        base.Awake();

        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
            Debug.LogError("ConsumableItem: Không tìm thấy PlayerStats trong Scene!");
    }

    public override void UseItem()
    {
        if (playerStats == null || EffectService.Instance == null)
        {
            Debug.LogWarning("Không thể dùng item: Thiếu PlayerStats hoặc EffectController.");
            return;
        }

        if (triggersGlobalPotionCooldown)
        {
            if (playerStats.IsPotionOnCooldown()) return;
        }

        bool canBeUsed = true;
        switch (effectID)
        {
            case "HEAL_INSTANT":
                if (!playerStats.CanHeal())
                {
                    canBeUsed = false;
                    Debug.Log("Không thể dùng Potion: HP đã đầy!");
                }
                break;
            case "MANA_INSTANT":
                if (!playerStats.CanRecoverMP())
                {
                    canBeUsed = false;
                    Debug.Log("Không thể dùng Potion: MP đã đầy!");
                }
                break;
        }

        if (!canBeUsed) return;

        float durationForIcon = effectDuration;
        if (triggersGlobalPotionCooldown)
        {
            durationForIcon = playerStats.potionCooldownDuration;
            playerStats.TriggerPotionCooldown();
        }

        EffectService.Instance.AddEffect(
            playerStats.gameObject,
            effectID,
            effectDuration,
            effectValue
        );

        RemoveFromStack(1);

        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.ScheduleConsumableSync(this.dbID, this.quantity);
            InventoryController.Instance.ReBuildItemCounts();
        }

        if (quantity <= 0)
        {
            Slot parentSlot = GetComponentInParent<Slot>();
            if (parentSlot != null)
            {
                parentSlot.currentItem = null;
            }

            Destroy(gameObject);
        }
    }
}
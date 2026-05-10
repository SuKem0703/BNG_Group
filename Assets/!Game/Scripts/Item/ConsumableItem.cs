using UnityEngine;
using Unity.Netcode;

public class ConsumableItem : Item
{
    public override ItemType ItemType => ItemType.Consumable;
    public override bool IsStackable => true;

    [Header("Consumable Effect")]
    public string effectID;
    public float effectValue;
    public float effectDuration = 0f;

    [Header("Cooldown")]
    public bool triggersGlobalPotionCooldown = false;

    private PlayerStats GetLocalPlayerStats()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var adapter = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<LocalPlayerAdapter>();
            if (adapter != null) return adapter.playerStats;
        }
        return null;
    }

    public override void UseItem()
    {
        PlayerStats playerStats = GetLocalPlayerStats();

        if (playerStats == null || EffectService.Instance == null)
        {
            Debug.LogWarning("Không thể dùng item: Thiếu PlayerStats của chủ sở hữu.");
            return;
        }

        if (triggersGlobalPotionCooldown && playerStats.IsPotionOnCooldown()) return;

        bool canBeUsed = true;
        switch (effectID)
        {
            case "HEAL_INSTANT":
                if (!playerStats.CanHeal()) { canBeUsed = false; GameNotify.Show("HP đã đầy!"); }
                break;
            case "MANA_INSTANT":
                if (!playerStats.CanRecoverMP()) { canBeUsed = false; GameNotify.Show("MP đã đầy!"); }
                break;
        }

        if (!canBeUsed) return;

        if (triggersGlobalPotionCooldown) playerStats.TriggerPotionCooldown();

        EffectService.Instance.AddEffect(playerStats.gameObject, effectID, effectDuration, effectValue);

        RemoveFromStack(1);

        if (InventoryController.Instance != null)
        {
            var ramData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == this.dbID);
            if (ramData != null) ramData.quantity = this.quantity;

            if (this.quantity <= 0)
            {
                InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == this.dbID);
                InventoryService.Instance.CancelQuantityUpdate(this.dbID);
                InventoryService.Instance.RequestRemoveItem(this.dbID);

                Slot parentSlot = GetComponentInParent<Slot>();
                if (parentSlot != null) parentSlot.currentItem = null;

                Destroy(gameObject);
            }
            else
            {
                InventoryService.Instance.ScheduleQuantityUpdate(this.dbID, this.quantity);
            }

            InventoryController.Instance.ReBuildItemCounts();
        }
    }
}
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

    private bool GetLocalPlayerComponents(out PlayerStats stats, out PlayerVitals vitals)
    {
        stats = null;
        vitals = null;

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient &&
            NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            GameObject playerObj = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
            stats = playerObj.GetComponent<PlayerStats>();
            vitals = playerObj.GetComponent<PlayerVitals>();

            return stats != null && vitals != null;
        }
        return false;
    }

    public override void UseItem()
    {
        if (!GetLocalPlayerComponents(out PlayerStats playerStats, out PlayerVitals playerVitals))
        {
            Debug.LogWarning("Không thể dùng item: Không tìm thấy PlayerStats hoặc PlayerVitals của chủ sở hữu.");
            return;
        }

        if (EffectService.Instance == null) return;

        if (triggersGlobalPotionCooldown && playerStats.IsPotionOnCooldown()) return;

        bool canBeUsed = true;
        switch (effectID)
        {
            case "HEAL_INSTANT":
                if (!playerVitals.CanHeal()) { canBeUsed = false; GameNotify.Show("HP đã đầy!"); }
                break;
            case "MANA_INSTANT":
                if (!playerVitals.CanRecoverMP()) { canBeUsed = false; GameNotify.Show("MP đã đầy!"); }
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
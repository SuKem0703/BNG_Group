using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryService : MonoBehaviour
{
    public static InventoryService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private string CurrentProfileId
    {
        get
        {
            string id = PlayerPrefs.GetString("CurrentProfileId", "");
            return string.IsNullOrEmpty(id) ? "OfflineUser" : id;
        }
    }

    #region Models & DTOs
    [System.Serializable]
    public class ServerUserItem
    {
        public int id;
        public int itemId;
        public int quantity;
        public int slotIndex;
        public bool isEquipped;
        public int rarity;
        public float qualityFactor;
    }

    [System.Serializable]
    public class StorageItemDTO
    {
        public int id;
        public string accountId;
        public string chestId;
        public int itemId;
        public int slotIndex;
        public int quantity;
        public int rarity;
        public float qualityFactor;
    }
    #endregion

    // Hàm kiểm tra tính hợp lệ của ItemId
    private bool IsValidItemId(int itemId)
    {
        if (itemId <= 0) return false;
        if (ItemDictionary.Instance != null && ItemDictionary.Instance.GetItemPrefab(itemId) == null) return false;
        return true;
    }

    #region Public API

    public void SyncInventory(System.Action<List<ServerUserItem>> onComplete)
    {
        var db = DatabaseManager.Instance.GetConnection();
        string profile = CurrentProfileId;

        var items = db.Table<InventoryItemEntity>()
            .Where(x => x.ProfileId == profile)
            .ToList()
            .Where(x => string.IsNullOrEmpty(x.ChestId))
            .ToList();

        var validItems = new List<ServerUserItem>();

        // Lọc và xóa các bản ghi có ItemId không hợp lệ khỏi SQLite
        foreach (var x in items)
        {
            if (!IsValidItemId(x.ItemId))
            {
                Debug.LogWarning($"[InventoryService] Phát hiện vật phẩm có ItemId không hợp lệ (DbId: {x.DbId}, ItemId: {x.ItemId}) trong túi đồ. Đã xóa khỏi Database!");
                db.Delete(x);
                continue;
            }

            validItems.Add(new ServerUserItem
            {
                id = x.DbId,
                itemId = x.ItemId,
                quantity = x.Quantity,
                slotIndex = x.SlotIndex,
                isEquipped = x.IsEquipped,
                rarity = x.Rarity,
                qualityFactor = x.QualityFactor
            });
        }

        onComplete?.Invoke(validItems);
    }

    public void RequestEquip(int itemDbId, bool isEquipped)
    {
        var db = DatabaseManager.Instance.GetConnection();
        var item = db.Find<InventoryItemEntity>(itemDbId);

        if (item != null && item.ProfileId == CurrentProfileId)
        {
            item.IsEquipped = isEquipped;
            db.Update(item);
        }
    }

    public void RequestMoveItem(int itemDbId, int newSlotIndex, bool isStackable, System.Action<bool> onComplete = null)
    {
        var db = DatabaseManager.Instance.GetConnection();
        var source = db.Find<InventoryItemEntity>(itemDbId);

        if (source != null)
        {
            // Nếu item trong DB có ItemId không hợp lệ thì xóa khỏi DB
            if (!IsValidItemId(source.ItemId))
            {
                Debug.LogWarning($"[InventoryService] Hủy di chuyển và xóa vật phẩm lỗi (DbId: {source.DbId}, ItemId: {source.ItemId}) khỏi Database!");
                db.Delete(source);
                onComplete?.Invoke(false);
                return;
            }

            bool success = PerformMoveOrSwap(itemDbId, source.ChestId, newSlotIndex, isStackable);
            onComplete?.Invoke(success);
        }
        else onComplete?.Invoke(false);
    }

    public void RequestUpdateQuantityImmediate(int dbId, int newQuantity)
    {
        var db = DatabaseManager.Instance.GetConnection();
        var item = db.Find<InventoryItemEntity>(dbId);

        if (item != null && item.ProfileId == CurrentProfileId)
        {
            if (!IsValidItemId(item.ItemId) || newQuantity <= 0)
            {
                if (!IsValidItemId(item.ItemId))
                    Debug.LogWarning($"[InventoryService] Xóa vật phẩm lỗi khi cập nhật số lượng (DbId: {item.DbId}, ItemId: {item.ItemId}).");
                db.Delete(item);
            }
            else
            {
                item.Quantity = newQuantity;
                db.Update(item);
            }
        }
    }

    public void RequestAddItem(int itemId, int quantity, int slotIndex, int rarity, float quality, uint validationSeed, bool isStackable, System.Action<int, string> onSuccess)
    {
        // Chặn thêm vật phẩm có ItemId không hợp lệ vào Database
        if (!IsValidItemId(itemId))
        {
            Debug.LogWarning($"[InventoryService] Từ chối thêm vật phẩm vào Database vì ItemId không hợp lệ (ItemId: {itemId}).");
            return;
        }

        var db = DatabaseManager.Instance.GetConnection();
        string profile = CurrentProfileId;

        if (isStackable)
        {
            var existing = db.Table<InventoryItemEntity>()
                .Where(x => x.ProfileId == profile && x.ItemId == itemId && x.SlotIndex < 2000)
                .ToList()
                .FirstOrDefault(x => string.IsNullOrEmpty(x.ChestId));

            if (existing != null)
            {
                existing.Quantity += quantity;
                db.Update(existing);
                onSuccess?.Invoke(existing.DbId, "stacked");
                return;
            }
        }

        var newItem = new InventoryItemEntity
        {
            ProfileId = profile,
            ItemId = itemId,
            Quantity = quantity,
            SlotIndex = slotIndex,
            IsEquipped = slotIndex >= 2000,
            Rarity = rarity,
            QualityFactor = quality,
            ChestId = null
        };

        db.Insert(newItem);
        onSuccess?.Invoke(newItem.DbId, "created");
    }

    public void RequestRemoveItem(int itemDbId, System.Action<bool> onComplete = null)
    {
        var db = DatabaseManager.Instance.GetConnection();
        var item = db.Find<InventoryItemEntity>(itemDbId);

        if (item != null && item.ProfileId == CurrentProfileId)
        {
            db.Delete(item);
            onComplete?.Invoke(true);
        }
        else onComplete?.Invoke(false);
    }

    public void RequestBuyItem(int itemId, int quantity, System.Action<bool, List<ServerUserItem>> onComplete)
    {
        if (!IsValidItemId(itemId))
        {
            Debug.LogWarning($"[InventoryService] Hủy mua vật phẩm vì ItemId không hợp lệ (ItemId: {itemId}).");
            onComplete?.Invoke(false, null);
            return;
        }

        RequestAddItem(itemId, quantity, 0, 1, 1f, 0, true, (dbId, action) =>
        {
            SyncInventory((items) => onComplete?.Invoke(true, items));
        });
    }

    #endregion

    #region Storage API

    public void RequestSyncChest(string chestId, System.Action<List<ServerUserItem>> onComplete)
    {
        var db = DatabaseManager.Instance.GetConnection();
        var items = db.Table<InventoryItemEntity>().Where(x => x.ProfileId == CurrentProfileId && x.ChestId == chestId).ToList();

        var validItems = new List<ServerUserItem>();

        // Lọc và xóa vật phẩm lỗi trong rương
        foreach (var x in items)
        {
            if (!IsValidItemId(x.ItemId))
            {
                Debug.LogWarning($"[InventoryService] Phát hiện vật phẩm lỗi trong rương {chestId} (DbId: {x.DbId}, ItemId: {x.ItemId}). Đã xóa khỏi Database!");
                db.Delete(x);
                continue;
            }

            validItems.Add(new ServerUserItem
            {
                id = x.DbId,
                itemId = x.ItemId,
                quantity = x.Quantity,
                slotIndex = x.SlotIndex,
                isEquipped = x.IsEquipped,
                rarity = x.Rarity,
                qualityFactor = x.QualityFactor
            });
        }

        onComplete?.Invoke(validItems);
    }

    public void RequestDeposit(int itemDbId, string chestId, int slotIndex, bool isStackable, System.Action<bool> onComplete)
    {
        bool success = PerformMoveOrSwap(itemDbId, chestId, slotIndex, isStackable);
        onComplete?.Invoke(success);
    }

    public void RequestWithdraw(int itemDbId, int targetSlotIndex, bool isStackable, System.Action<bool> onComplete)
    {
        bool success = PerformMoveOrSwap(itemDbId, null, targetSlotIndex, isStackable);
        onComplete?.Invoke(success);
    }

    public void RequestLoadMapStorage(string sceneName, System.Action<List<StorageItemDTO>> onComplete)
    {
        onComplete?.Invoke(new List<StorageItemDTO>());
    }

    public void RequestLoadSingleChest(string chestId, System.Action<List<StorageItemDTO>> onComplete)
    {
        var db = DatabaseManager.Instance.GetConnection();
        var items = db.Table<InventoryItemEntity>().Where(x => x.ProfileId == CurrentProfileId && x.ChestId == chestId).ToList();

        var validItems = new List<StorageItemDTO>();

        // Lọc và xóa vật phẩm lỗi khi tải rương đơn
        foreach (var x in items)
        {
            if (!IsValidItemId(x.ItemId))
            {
                Debug.LogWarning($"[InventoryService] Phát hiện vật phẩm lỗi khi mở rương {chestId} (DbId: {x.DbId}, ItemId: {x.ItemId}). Đã xóa khỏi Database!");
                db.Delete(x);
                continue;
            }

            validItems.Add(new StorageItemDTO
            {
                id = x.DbId,
                accountId = x.ProfileId,
                chestId = x.ChestId,
                itemId = x.ItemId,
                slotIndex = x.SlotIndex,
                quantity = x.Quantity,
                rarity = x.Rarity,
                qualityFactor = x.QualityFactor
            });
        }

        onComplete?.Invoke(validItems);
    }

    #endregion

    #region Core Move / Swap Logic

    private bool PerformMoveOrSwap(int itemDbId, string targetChestId, int newSlotIndex, bool isStackable)
    {
        var db = DatabaseManager.Instance.GetConnection();
        string profile = CurrentProfileId;
        bool success = false;

        db.RunInTransaction(() =>
        {
            var source = db.Find<InventoryItemEntity>(itemDbId);
            if (source == null || source.ProfileId != profile) return;

            if (!IsValidItemId(source.ItemId))
            {
                Debug.LogWarning($"[InventoryService] Xóa vật phẩm nguồn lỗi khi Move/Swap (DbId: {source.DbId}, ItemId: {source.ItemId}).");
                db.Delete(source);
                return;
            }

            string targetChest = string.IsNullOrEmpty(targetChestId) ? null : targetChestId;
            string sourceChest = string.IsNullOrEmpty(source.ChestId) ? null : source.ChestId;

            var target = db.Table<InventoryItemEntity>()
                .Where(x => x.ProfileId == profile && x.SlotIndex == newSlotIndex)
                .ToList()
                .FirstOrDefault(x => (string.IsNullOrEmpty(x.ChestId) ? null : x.ChestId) == targetChest);

            if (target != null && !IsValidItemId(target.ItemId))
            {
                Debug.LogWarning($"[InventoryService] Xóa vật phẩm đích lỗi khi Move/Swap (DbId: {target.DbId}, ItemId: {target.ItemId}).");
                db.Delete(target);
                target = null;
            }

            if (target == null)
            {
                source.SlotIndex = newSlotIndex;
                source.ChestId = targetChest;
                source.IsEquipped = (targetChest == null && newSlotIndex >= 2000);
                db.Update(source);
                success = true;
            }
            else
            {
                if (isStackable && target.ItemId == source.ItemId)
                {
                    target.Quantity += source.Quantity;
                    db.Update(target);
                    db.Delete(source);
                    success = true;
                }
                else
                {
                    int oldSlot = source.SlotIndex;
                    string oldChest = sourceChest;

                    target.SlotIndex = oldSlot;
                    target.ChestId = oldChest;
                    target.IsEquipped = (oldChest == null && oldSlot >= 2000);

                    source.SlotIndex = newSlotIndex;
                    source.ChestId = targetChest;
                    source.IsEquipped = (targetChest == null && newSlotIndex >= 2000);

                    db.Update(target);
                    db.Update(source);
                    success = true;
                }
            }
        });
        return success;
    }

    #endregion

    #region Sync Queue

    public void ScheduleQuantityUpdate(int itemDbId, int newQuantity, float delay = 2.0f)
    {
        if (itemDbId <= 0) return;
        RequestUpdateQuantityImmediate(itemDbId, newQuantity);
    }

    public void CancelQuantityUpdate(int itemDbId) { }

    public void ForceSyncPendingQuantities() { }

    #endregion

    #region Move Item

    public void ScheduleMoveItem(int itemDbId, int newSlotIndex)
    {
        if (itemDbId <= 0) return;
        RequestMoveItem(itemDbId, newSlotIndex, false);
    }

    public void ForceSyncPendingMoves() { }

    #endregion
}
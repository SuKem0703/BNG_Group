using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InventoryService : MonoBehaviour
{
    #region Singleton
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
    #endregion

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

    [System.Serializable] public class EquipRequestDTO { public int ItemDbId; public bool IsEquipped; }
    [System.Serializable] public class MoveRequestDTO { public int ItemDbId; public int NewSlotIndex; public bool IsStackable { get; set; } }

    [System.Serializable]
    public class BuyRequestDTO
    {
        public int ItemId;
        public int Quantity;
        public int Price;
        public string Currency;
        public bool IsStackable;
    }

    [System.Serializable]
    public class StorageItemDTO
    {
        public int id;          // Storage ID
        public string accountId;
        public string chestId;
        public int itemId;
        public int slotIndex;
        public int quantity;
        public int rarity;
        public float qualityFactor;
    }
    [System.Serializable] public class UpdateQtyRequestDTO { public int ItemDbId; public int NewQuantity; }

    [System.Serializable]
    public class AddItemRequestDTO
    {
        public int ItemId;
        public int Quantity;
        public int SlotIndex;
        public int Rarity;
        public float QualityFactor;
    }

    [System.Serializable]
    public class AddItemResponse
    {
        public bool success;
        public int newDbId;
    }

    [System.Serializable]
    public class RemoveRequestDTO { public int ItemDbId; }

    [System.Serializable] public class DepositDTO { public int ItemDbId; public string ChestId; public int SlotIndex; public bool IsStackable { get; set; } }
    [System.Serializable] public class WithdrawDTO { public int ItemDbId; public int SlotIndex; public bool IsStackable { get; set; } }

    private class InventoryWrapper
    {
        public List<ServerUserItem> items;
    }

    private class StorageListWrapper
    {
        public List<StorageItemDTO> items;
    }

    #endregion

    #region Public API (Game gọi)

    public void SyncInventory(System.Action<List<ServerUserItem>> onComplete)
    {
        StartCoroutine(SyncRoutine(onComplete));
    }

    public void RequestEquip(int itemDbId, bool isEquipped)
    {
        StartCoroutine(PostRequest(
            "api/Inventory/equip",
            new EquipRequestDTO { ItemDbId = itemDbId, IsEquipped = isEquipped },
            null
        ));
    }

    public void RequestMoveItem(int itemDbId, int newSlotIndex, bool isStackable, System.Action<bool> onComplete = null)
    {
        var body = new MoveRequestDTO
        {
            ItemDbId = itemDbId,
            NewSlotIndex = newSlotIndex,
            IsStackable = isStackable
        };

        StartCoroutine(PostRequest("api/Inventory/move", body, onComplete));
    }

    public void RequestUpdateQuantity(int dbId, int newQuantity)
    {
        StartCoroutine(PostRequest(
            "api/Inventory/update-quantity",
            new UpdateQtyRequestDTO { ItemDbId = dbId, NewQuantity = newQuantity },
            null
        ));
    }

    public void RequestBuyItem(int itemId, int quantity, int price, string currency, bool isStackable, System.Action<bool, List<ServerUserItem>> onComplete)
    {
        StartCoroutine(BuyRoutine(itemId, quantity, price, currency, isStackable, onComplete));
    }

    public void RequestAddItem(
        int itemId,
        int quantity,
        int slotIndex,
        int rarity,
        float quality,
        System.Action<int> onSuccess)
    {
        StartCoroutine(AddItemRoutine(itemId, quantity, slotIndex, rarity, quality, onSuccess));
    }

    public void RequestRemoveItem(int itemDbId)
    {
        StartCoroutine(PostRequest("api/Inventory/remove", new RemoveRequestDTO { ItemDbId = itemDbId }, null));
    }

    // Lấy đồ trong rương
    public void RequestSyncChest(string chestId, System.Action<List<ServerUserItem>> onComplete)
    {
        string url = NetworkConfig.GetUrl($"api/Storage/sync/{chestId}");
        StartCoroutine(GetRequestList(url, onComplete));
    }

    // Cất đồ (Deposit)
    public void RequestDeposit(int itemDbId, string chestId, int slotIndex, bool isStackable, System.Action<bool> onComplete)
    {
        var body = new DepositDTO { ItemDbId = itemDbId, ChestId = chestId, SlotIndex = slotIndex, IsStackable = isStackable };
        StartCoroutine(PostRequest("api/Storage/deposit", body, onComplete));
    }

    // Rút đồ (Withdraw)
    public void RequestWithdraw(int itemDbId, int targetSlotIndex, bool isStackable, System.Action<bool> onComplete)
    {
        var body = new WithdrawDTO { ItemDbId = itemDbId, SlotIndex = targetSlotIndex, IsStackable = isStackable };
        StartCoroutine(PostRequest("api/Storage/withdraw", body, onComplete));
    }

    public void RequestLoadMapStorage(string sceneName, System.Action<List<StorageItemDTO>> onComplete)
    {
        // sceneName ví dụ: "Map1" (Server sẽ tìm "Map1_%")
        string url = NetworkConfig.GetUrl($"api/Storage/load-map-storage?sceneName={sceneName}");
        StartCoroutine(GetStorageList(url, onComplete));
    }

    // Gọi khi muốn refresh 1 rương cụ thể (sau khi Deposit/Withdraw)
    public void RequestLoadSingleChest(string chestId, System.Action<List<StorageItemDTO>> onComplete)
    {
        string url = NetworkConfig.GetUrl($"api/Storage/load-chest?chestId={chestId}");
        StartCoroutine(GetStorageList(url, onComplete));
    }

    #endregion

    #region Network Coroutines

    private IEnumerator SyncRoutine(System.Action<List<ServerUserItem>> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Inventory/sync");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"items\":" + request.downloadHandler.text + "}";
            try
            {
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
                onComplete?.Invoke(wrapper.items);
            }
            catch
            {
                Debug.LogWarning("[InventoryService] Invalid sync JSON");
                onComplete?.Invoke(new List<ServerUserItem>());
            }
        }
        else
        {
            Debug.LogError($"[InventoryService] Sync failed: {request.error}");
            onComplete?.Invoke(null);
        }
    }

    private IEnumerator BuyRoutine(int itemId, int quantity, int price, string currency, bool isStackable, System.Action<bool, List<ServerUserItem>> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Shop/buy");
        string token = PlayerPrefs.GetString("AuthToken", "");

        // Đóng gói dữ liệu gửi đi
        var body = new BuyRequestDTO
        {
            ItemId = itemId,
            Quantity = quantity,
            Price = price,
            Currency = currency,
            IsStackable = isStackable
        };

        string json = JsonUtility.ToJson(body);

        Debug.Log($"[Client Shop] Gửi yêu cầu mua: {json}");

        UnityWebRequest request = CreatePostRequest(url, token, json);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string wrapped = "{\"items\":" + request.downloadHandler.text + "}";
            try
            {
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(wrapped);
                onComplete?.Invoke(true, wrapper.items);
            }
            catch
            {
                onComplete?.Invoke(true, null);
            }
        }
        else
        {
            Debug.LogError($"[InventoryService] Buy failed: {request.downloadHandler.text}");
            onComplete?.Invoke(false, null);
        }
    }

    private IEnumerator AddItemRoutine(
        int itemId,
        int quantity,
        int slotIndex,
        int rarity,
        float quality,
        System.Action<int> onSuccess)
    {
        string url = NetworkConfig.GetUrl("api/Inventory/add");
        string token = PlayerPrefs.GetString("AuthToken", "");

        var body = new AddItemRequestDTO
        {
            ItemId = itemId,
            Quantity = quantity,
            SlotIndex = slotIndex,
            Rarity = rarity,
            QualityFactor = quality
        };

        UnityWebRequest request = CreatePostRequest(url, token, JsonUtility.ToJson(body));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AddItemResponse>(request.downloadHandler.text);
            if (res.success) onSuccess?.Invoke(res.newDbId);
        }
        else
        {
            Debug.LogError($"[InventoryService] Add item failed: {request.downloadHandler.text}");
        }
    }

    #endregion

    #region Helpers

    private IEnumerator PostRequest(string endpoint, object body, System.Action<bool> onComplete)
    {
        string url = NetworkConfig.GetUrl(endpoint);
        string token = PlayerPrefs.GetString("AuthToken", "");
        string json = JsonUtility.ToJson(body);

        UnityWebRequest request = CreatePostRequest(url, token, json);
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        if (!success)
        {
            Debug.LogError($"[InventoryService] {endpoint} failed: {request.downloadHandler.text}");
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API {url} Failed [{request.responseCode}]: {request.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }

        onComplete?.Invoke(success);
    }

    private UnityWebRequest CreatePostRequest(string url, string token, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");
        return request;
    }

    private IEnumerator GetRequestList(string url, System.Action<List<ServerUserItem>> onComplete)
    {
        string token = PlayerPrefs.GetString("AuthToken", "");
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"items\":" + request.downloadHandler.text + "}";
            try
            {
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
                onComplete?.Invoke(wrapper.items);
            }
            catch { onComplete?.Invoke(new List<ServerUserItem>()); }
        }
        else { onComplete?.Invoke(null); }
    }

    private IEnumerator GetStorageList(string url, System.Action<List<StorageItemDTO>> onComplete)
    {
        string token = PlayerPrefs.GetString("AuthToken", "");
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Bọc JSON lại vì API trả về mảng []
            string json = "{\"items\":" + request.downloadHandler.text + "}";
            try
            {
                var wrapper = JsonUtility.FromJson<StorageListWrapper>(json);
                onComplete?.Invoke(wrapper.items);
            }
            catch
            {
                // Nếu rương rỗng hoặc lỗi parse
                onComplete?.Invoke(new List<StorageItemDTO>());
            }
        }
        else
        {
            Debug.LogError($"Load Storage Failed: {request.error}");
            onComplete?.Invoke(null);
        }
    }

    #endregion
}
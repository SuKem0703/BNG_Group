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

    [System.Serializable] public class EquipRequestDTO { public int itemDbId; public bool isEquipped; }
    [System.Serializable] public class MoveRequestDTO { public int itemDbId; public int newSlotIndex; public bool isStackable; }

    [System.Serializable]
    public class BuyRequestDTO
    {
        public int itemId;
        public int quantity;
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
    [System.Serializable] public class UpdateQtyRequestDTO { public int itemDbId; public int newQuantity; }

    [System.Serializable]
    public class AddItemRequestDTO
    {
        public int itemId;
        public int quantity;
        public int slotIndex;
        public int rarity;
        public float qualityFactor;
    }

    [System.Serializable]
    public class AddItemResponse
    {
        public bool success;
        public int dbId;
    }

    [System.Serializable]
    public class RemoveRequestDTO { public int itemDbId; }

    [System.Serializable] public class DepositDTO { public int itemDbId; public string chestId; public int slotIndex; public bool isStackable; }
    [System.Serializable] public class WithdrawDTO { public int itemDbId; public int slotIndex; public bool isStackable; }

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
            new EquipRequestDTO { itemDbId = itemDbId, isEquipped = isEquipped },
            null
        ));
    }

    public void RequestMoveItem(int itemDbId, int newSlotIndex, bool isStackable, System.Action<bool> onComplete = null)
    {
        var body = new MoveRequestDTO
        {
            itemDbId = itemDbId,
            newSlotIndex = newSlotIndex,
            isStackable = isStackable
        };

        StartCoroutine(PostRequest("api/Inventory/move", body, onComplete));
    }

    public void RequestUpdateQuantity(int dbId, int newQuantity)
    {
        StartCoroutine(PostRequest(
            "api/Inventory/update-quantity",
            new UpdateQtyRequestDTO { itemDbId = dbId, newQuantity = newQuantity },
            null
        ));
    }

    public void RequestBuyItem(int itemId, int quantity, System.Action<bool, List<ServerUserItem>> onComplete)
    {
        StartCoroutine(BuyRoutine(itemId, quantity, onComplete));
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

    public void RequestRemoveItem(int itemDbId, System.Action<bool> onComplete = null)
    {
        StartCoroutine(PostRequest("api/Inventory/remove", new RemoveRequestDTO { itemDbId = itemDbId }, onComplete));
    }

    public void RequestSyncChest(string chestId, System.Action<List<ServerUserItem>> onComplete)
    {
        string url = NetworkConfig.GetUrl($"api/Storage/sync/{chestId}");
        StartCoroutine(GetRequestList(url, onComplete));
    }

    public void RequestDeposit(int itemDbId, string chestId, int slotIndex, bool isStackable, System.Action<bool> onComplete)
    {
        var body = new DepositDTO { itemDbId = itemDbId, chestId = chestId, slotIndex = slotIndex, isStackable = isStackable };
        StartCoroutine(PostRequest("api/Storage/deposit", body, onComplete));
    }

    public void RequestWithdraw(int itemDbId, int targetSlotIndex, bool isStackable, System.Action<bool> onComplete)
    {
        var body = new WithdrawDTO { itemDbId = itemDbId, slotIndex = targetSlotIndex, isStackable = isStackable };
        StartCoroutine(PostRequest("api/Storage/withdraw", body, onComplete));
    }

    public void RequestLoadMapStorage(string sceneName, System.Action<List<StorageItemDTO>> onComplete)
    {
        string url = NetworkConfig.GetUrl($"api/Storage/load-map-storage?sceneName={sceneName}");
        StartCoroutine(GetStorageList(url, onComplete));
    }

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

        float startTime = Time.realtimeSinceStartup;

        yield return request.SendWebRequest();

        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

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

    private IEnumerator BuyRoutine(int itemId, int quantity, System.Action<bool, List<ServerUserItem>> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Shop/buy");
        string token = PlayerPrefs.GetString("AuthToken", "");

        var body = new BuyRequestDTO
        {
            itemId = itemId,
            quantity = quantity
        };

        string json = JsonUtility.ToJson(body);

        Debug.Log($"[Client Shop] Gửi yêu cầu mua: {json}");

        UnityWebRequest request = CreatePostRequest(url, token, json);

        float startTime = Time.realtimeSinceStartup;

        yield return request.SendWebRequest();

        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

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
            itemId = itemId,
            quantity = quantity,
            slotIndex = slotIndex,
            rarity = rarity,
            qualityFactor = quality
        };

        UnityWebRequest request = CreatePostRequest(url, token, JsonUtility.ToJson(body));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<AddItemResponse>(request.downloadHandler.text);
            if (res.success) onSuccess?.Invoke(res.dbId);
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

        float startTime = Time.realtimeSinceStartup;

        yield return request.SendWebRequest();

        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

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

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

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

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"items\":" + request.downloadHandler.text + "}";
            try
            {
                var wrapper = JsonUtility.FromJson<StorageListWrapper>(json);
                onComplete?.Invoke(wrapper.items);
            }
            catch
            {
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
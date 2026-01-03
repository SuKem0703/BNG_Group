using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StorageMapLoader : MonoBehaviour
{
    public static StorageMapLoader Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Tự động load khi vào scene
        LoadAllChestsInScene();
    }

    public void LoadAllChestsInScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // 1. Tìm tất cả rương đang có trong Scene
        StorageChest[] allChests = FindObjectsByType<StorageChest>(FindObjectsSortMode.None);

        // Tạo Dictionary để tra cứu rương nhanh theo ID
        Dictionary<string, StorageChest> chestMap = new Dictionary<string, StorageChest>();
        foreach (var chest in allChests)
        {
            if (!string.IsNullOrEmpty(chest.chestID))
            {
                chest.ClearCache(); // Xóa cache cũ
                if (!chestMap.ContainsKey(chest.chestID))
                    chestMap.Add(chest.chestID, chest);
            }
        }

        Debug.Log($"[StorageLoader] Bắt đầu tải dữ liệu cho {allChests.Length} rương tại map {currentScene}...");

        // 2. Gọi API Load Map
        InventoryService.Instance.RequestLoadMapStorage(currentScene, (serverItems) =>
        {
            if (serverItems == null) return;

            // 3. Phân phát Item về đúng rương
            int count = 0;
            foreach (var itemDTO in serverItems)
            {
                if (chestMap.TryGetValue(itemDTO.chestId, out StorageChest targetChest))
                {
                    targetChest.AddToCache(itemDTO);
                    count++;
                }
            }

            Debug.Log($"[StorageLoader] Đã phân phát {count} vật phẩm vào các rương.");
        });
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StorageChestController : MonoBehaviour
{
    public static StorageChestController Instance { get; private set; }

    [Header("UI References")]
    public GameObject chestPanel;
    public GameObject slotPrefab;
    public int slotCount = 20;

    private ItemDictionary itemDictionary;
    private StorageChest currentActiveChest;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (chestPanel == null)
        {
            chestPanel = GameObject.Find("StorageChestPanel");

            if (chestPanel == null)
            {
                Debug.LogWarning("Không tìm thấy chestPanel, StorageChestController sẽ chạy headless.");
            }
            else
            {
                chestPanel.SetActive(false);
            }
        }

        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        if (itemDictionary == null)
            Debug.LogError("ItemDictionary NOT FOUND!");

        chestPanel.SetActive(false);
    }

    void Start()
    {
        BuildSlots();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void OpenChest(StorageChest chest)
    {
        if (chestPanel.activeSelf) return;

        if (!GameStateManager.CanProcessInput()) return;

        currentActiveChest = chest;

        ClearChestUI();
        PopulateChest(chest.chestData);

        chestPanel.SetActive(true);

        MenuStateManager.Instance.OpenMenu(chestPanel);
    }

    public void CloseChest()
    {
        if (currentActiveChest != null)
        {
            SyncDataToChest();
        }

        currentActiveChest = null;
        chestPanel.SetActive(false);

        MenuStateManager.Instance.CloseCurrentMenu();
    }

    private void ClearChestUI()
    {
        foreach (Transform slot in chestPanel.transform)
        {
            Slot s = slot.GetComponent<Slot>();
            if (s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }
    }

    public void SyncDataIfOpen(StorageChest chest)
    {
        if (currentActiveChest == chest && chestPanel.activeSelf)
            SyncDataToChest();
    }

    private void BuildSlots()
    {
        foreach (Transform child in chestPanel.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < slotCount; i++)
            Instantiate(slotPrefab, chestPanel.transform);
    }

    private void PopulateChest(List<InventorySaveData> savedData)
    {
        if (savedData == null || savedData.Count == 0) return;

        foreach (var data in savedData)
        {
            if (data.slotIndex >= slotCount) continue;

            Transform slotTf = chestPanel.transform.GetChild(data.slotIndex);
            Slot slot = slotTf.GetComponent<Slot>();

            GameObject prefab = itemDictionary.GetItemPrefab(data.itemID);
            if (prefab == null) continue;

            GameObject itemObj = Instantiate(prefab, slot.transform);
            itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Item item = itemObj.GetComponent<Item>();
            item.quantity = data.quantity;
            item.isEquipped = data.isEquipped;
            item.rarity = data.rarity;
            item.qualityFactor = data.qualityFactor;

            if (item.quantity > 1)
                item.UpdateQuantityDisplay();

            slot.currentItem = itemObj;
        }
    }

    private void SyncDataToChest()
    {
        if (currentActiveChest == null) return;

        List<InventorySaveData> newData = new();

        for (int i = 0; i < chestPanel.transform.childCount; i++)
        {
            Slot slot = chestPanel.transform.GetChild(i).GetComponent<Slot>();
            if (slot.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null) continue;

            newData.Add(new InventorySaveData
            {
                itemID = item.ID,
                slotIndex = i,
                quantity = item.quantity,
                rarity = item.rarity,
                isEquipped = item.isEquipped,
                qualityFactor = item.qualityFactor
            });
        }

        currentActiveChest.chestData = newData;
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; }

    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount = 9;

    private Key[] hotbarKeys;

    private void Awake()
    {
        // Init Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        hotbarKeys = new Key[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
        }

        if (hotbarPanel.transform.childCount < slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, hotbarPanel.transform);
                Slot slot = slotObj.GetComponent<Slot>();
                slot.isHotBarSlot = true;
            }
        }
    }

    private void Start()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= RedrawHotbar;
            InventoryController.Instance.OnInventoryChanged += RedrawHotbar;
        }
    }

    private void OnEnable()
    {
        if (InventoryController.Instance != null)
            InventoryController.Instance.OnInventoryChanged += RedrawHotbar;
    }

    private void OnDisable()
    {
        if (InventoryController.Instance != null)
            InventoryController.Instance.OnInventoryChanged -= RedrawHotbar;
    }

    void Update()
    {
        if (PauseController.IsGamePause)
            return;

        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                UseItemInSlot(i);
            }
        }
    }

    void UseItemInSlot(int index)
    {
        if (index >= hotbarPanel.transform.childCount) return;

        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot.currentItem != null)
        {
            Item item = slot.currentItem.GetComponent<Item>();

            if (item.dbID == 0)
            {
                GameNotify.Show("Vật phẩm đang đồng bộ, vui lòng chờ!");
                return;
            }

            if (item is ConsumableItem consumable)
            {
                consumable.UseItem();
            }
            else if (item is SeedItem seedItem)
            {
                QuickPlantNearest(seedItem, slot);
            }
            else if (item is ItemTool toolItem)
            {
                toolItem.UseItem();
            }
        }
    }

    private void QuickPlantNearest(SeedItem seedItem, Slot slot)
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null) return;

        float checkRadius = 2f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, checkRadius);

        FarmPlot closestPlot = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            FarmPlot plot = col.GetComponent<FarmPlot>();

            if (plot != null && !plot.isPlanted && InteractionDetector.Instance.IsPlotInRange(plot))
            {
                float distance = Vector2.Distance(playerTransform.position, plot.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlot = plot;
                }
            }
        }

        if (closestPlot != null)
        {
            bool success = seedItem.TryPlantSeed(closestPlot);

            if (success)
            {
                if (seedItem.quantity <= 0)
                {
                    InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == seedItem.dbID);
                    Destroy(slot.currentItem);
                    slot.currentItem = null;
                }

                InventoryController.Instance.ReBuildItemCounts();
            }
        }
        else
        {
            GameNotify.Show("Không có ô đất trống hợp lệ ở gần!");
        }
    }

    private Transform GetPlayerTransform()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsConnectedClient &&
            Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.transform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("PlayerController");
        return player != null ? player.transform : null;
    }

    private void RedrawHotbar(List<InventorySaveData> inventoryData, int maxSlotCount)
    {
        foreach (Transform slotTrans in hotbarPanel.transform)
        {
            Slot s = slotTrans.GetComponent<Slot>();
            if (s != null && s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }

        if (inventoryData == null) return;

        foreach (var data in inventoryData)
        {
            if (data.slotIndex >= 1000 && data.slotIndex < 2000)
            {
                int localIndex = data.slotIndex - 1000;

                if (localIndex >= 0 && localIndex < hotbarPanel.transform.childCount)
                {
                    Slot slot = hotbarPanel.transform.GetChild(localIndex).GetComponent<Slot>();
                    GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);

                    if (itemPrefab != null)
                    {
                        GameObject itemObj = Instantiate(itemPrefab, slot.transform);
                        itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                        Item itemComponent = itemObj.GetComponent<Item>();
                        if (itemComponent != null)
                        {
                            itemComponent.dbID = data.dbID;
                            itemComponent.quantity = data.quantity;
                            itemComponent.rarity = data.rarity;
                            itemComponent.qualityFactor = data.qualityFactor;

                            itemComponent.UpdateQuantityDisplay();
                        }
                        slot.currentItem = itemObj;
                    }
                }
            }
        }
    }
}
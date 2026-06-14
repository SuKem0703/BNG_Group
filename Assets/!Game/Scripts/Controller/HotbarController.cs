using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; }

    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public GameObject skillPrefab;
    public int slotCount = 9;

    private Key[] hotbarKeys;
    private Dictionary<int, SkillData> assignedSkills = new Dictionary<int, SkillData>();

    public bool isAssigningMode { get; private set; }
    private SkillData skillToAssign;

    private void Awake()
    {
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
        if (PauseController.IsGamePause) return;

        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                UseSlot(i);
            }
        }
    }

    public void EnterAssignMode(SkillData skill)
    {
        isAssigningMode = true;
        skillToAssign = skill;
        GameNotify.Show("Chọn một ô trống trên Hotbar để gán kỹ năng.");
    }

    public void CancelAssignMode()
    {
        isAssigningMode = false;
        skillToAssign = null;
    }

    public void HandleSlotClick(int index, PointerEventData eventData)
    {
        if (isAssigningMode)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            Slot targetSlot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
            
            if (targetSlot.currentItem != null)
            {
                GameNotify.Show("Ô này đã có dữ liệu, vui lòng chọn ô trống!");
                return;
            }

            AssignSkillToSlot(index, skillToAssign);
            isAssigningMode = false;
            skillToAssign = null;
            GameNotify.Show("Gán kỹ năng thành công!");
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            UseSlot(index);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            Slot targetSlot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
            if (targetSlot.currentItem != null && targetSlot.currentItem.GetComponent<SkillItem>() != null)
            {
                RemoveSkillFromSlot(index);
            }
        }
    }

    void UseSlot(int index)
    {
        if (index >= hotbarPanel.transform.childCount) return;

        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        
        if (slot.currentItem != null)
        {
            SkillItem skillItem = slot.currentItem.GetComponent<SkillItem>();
            if (skillItem != null)
            {
                GameNotify.Show("Kích hoạt kỹ năng: " + skillItem.skillID);
                return;
            }

            Item item = slot.currentItem.GetComponent<Item>();
            if (item != null)
            {
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
    }

    public void AssignSkillToSlot(int index, SkillData skillData)
    {
        if (index < 0 || index >= hotbarPanel.transform.childCount || skillData == null) return;

        List<int> keysToRemove = new List<int>();
        foreach (var kvp in assignedSkills)
        {
            if (kvp.Value.skillID == skillData.skillID)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (int key in keysToRemove)
        {
            RemoveSkillFromSlot(key);
        }

        Slot targetSlot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();

        assignedSkills[index] = skillData;
        SpawnSkillItemInSlot(targetSlot, skillData);
        
        if (SaveController.Instance != null) SaveController.Instance.TriggerAutoSave();
    }

    public void RemoveSkillFromSlot(int index)
    {
        if (assignedSkills.ContainsKey(index))
        {
            assignedSkills.Remove(index);
            Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
            if (slot.currentItem != null && slot.currentItem.GetComponent<SkillItem>() != null)
            {
                Destroy(slot.currentItem);
                slot.currentItem = null;
            }
            if (SaveController.Instance != null) SaveController.Instance.TriggerAutoSave();
        }
    }

    private void SpawnSkillItemInSlot(Slot slot, SkillData skillData)
    {
        if (skillPrefab != null)
        {
            GameObject skillObj = Instantiate(skillPrefab, slot.transform);
            skillObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            SkillItem skillComp = skillObj.GetComponent<SkillItem>();
            if (skillComp != null)
            {
                skillComp.Setup(skillData);
            }

            slot.currentItem = skillObj;
        }
    }

    public Dictionary<int, string> GetHotbarSkillsSaveData()
    {
        Dictionary<int, string> data = new Dictionary<int, string>();
        foreach (var kvp in assignedSkills)
        {
            data[kvp.Key] = kvp.Value.skillID;
        }
        return data;
    }

    public void LoadHotbarSkillsSaveData(Dictionary<int, SkillData> loadedSkills)
    {
        assignedSkills.Clear();
        foreach (var kvp in loadedSkills)
        {
            assignedSkills[kvp.Key] = kvp.Value;
        }
        RedrawHotbar(InventoryController.Instance != null ? InventoryController.Instance.GetInventoryItemsData() : null, slotCount);
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

        if (inventoryData != null)
        {
            foreach (var data in inventoryData)
            {
                if (data.slotIndex >= 1000 && data.slotIndex < 2000)
                {
                    int localIndex = data.slotIndex - 1000;

                    if (localIndex >= 0 && localIndex < hotbarPanel.transform.childCount)
                    {
                        if (assignedSkills.ContainsKey(localIndex))
                        {
                            assignedSkills.Remove(localIndex);
                        }

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

        foreach (var kvp in assignedSkills)
        {
            int localIndex = kvp.Key;
            if (localIndex >= 0 && localIndex < hotbarPanel.transform.childCount)
            {
                Slot slot = hotbarPanel.transform.GetChild(localIndex).GetComponent<Slot>();
                if (slot.currentItem == null)
                {
                    SpawnSkillItemInSlot(slot, kvp.Value);
                }
            }
        }
    }
}
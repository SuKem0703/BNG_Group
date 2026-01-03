using NUnit.Framework.Interfaces;
using UnityEngine;

public class ItemInteractable : MonoBehaviour, IInteractable, ITargetableInfo
{
    private InventoryController inventoryController;
    private EquipmentScrollViewController equipmentViewController;

    private bool isCollected = false;

    void Start()
    {
        inventoryController = InventoryController.Instance;
        equipmentViewController = Object.FindFirstObjectByType<EquipmentScrollViewController>();
    }

    public bool CanInteract()
    {
        if (PauseController.IsGamePause || isCollected)
            return false;

        return true;
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        Item item = GetComponent<Item>();
        if (item == null) return;

        if (inventoryController == null)
        {
            Debug.LogError("ItemInteractable: Không tìm thấy InventoryController!");
            return;
        }

        Collectible questItem = GetComponent<Collectible>();
        Monologue monologue = GetComponent<Monologue>();

        if (monologue != null)
        {
            monologue.OnDialogueEndEvent += () => HandleMonologueEnd(monologue);
            monologue.OpenDialogOnTrigger();
            return;
        }

        HandleCollection(item, questItem);
    }

    private void HandleMonologueEnd(Monologue monologue)
    {
        monologue.OnDialogueEndEvent -= () => HandleMonologueEnd(monologue);

        Item item = GetComponent<Item>();
        Collectible questItem = GetComponent<Collectible>();

        HandleCollection(item, questItem);
    }

    private void HandleCollection(Item item, Collectible questItem)
    {
        if (isCollected) return;

        bool itemAdded = inventoryController.AddItem(gameObject);
        if (!itemAdded)
        {
            Debug.Log("Inventory đầy, không thể nhặt " + item.Name);
            ShowNotification("Túi đồ đã đầy, không thể nhặt thêm.");
            return;
        }

        isCollected = true;

        item.ShowPopUp();
        if (equipmentViewController != null)
            equipmentViewController.ShowEquipmentItems();

        if (questItem != null)
        {
            questItem.OnPickedUp();
        }
        else
        {
            if (SaveController.Instance != null)
                SaveController.Instance.TriggerAutoSave();

            Destroy(gameObject);
        }
    }
    private void ShowNotification(string message)
    {
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.NotifyUIPrefab != null)
        {
            GameObject notifyUIObj = Instantiate(LoadResourceManager.Instance.NotifyUIPrefab);
            NotifyUIController notifyUI = notifyUIObj.GetComponent<NotifyUIController>();
            if (notifyUI != null) notifyUI.Show(message);
        }
    }
    public TargetInfoData GetInfo()
    {
        Item itemData = GetComponent<Item>();
        if (itemData != null)
        {
            return new TargetInfoData(
                itemData.Name,
                itemData.icon,
                "Nhặt",
                TargetType.Item,
                itemData.rarity
            );
        }

        return new TargetInfoData("Item", null, "Nhặt", TargetType.Item);
    }
}
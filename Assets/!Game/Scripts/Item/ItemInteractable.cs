using NUnit.Framework.Interfaces;
using UnityEngine;
// Gán vào item để có thể nhặt được
public class ItemInteractable : MonoBehaviour, IInteractable, ITargetableInfo
{
    private InventoryController inventoryController;
    private EquipmentScrollViewController equipmentViewController;
    private SaveController saveController;

    private bool isCollected = false;

    void Start()
    {
        inventoryController = InventoryController.Instance;
        equipmentViewController = Object.FindFirstObjectByType<EquipmentScrollViewController>();
        saveController = Object.FindFirstObjectByType<SaveController>();
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

        Collectible collectible = GetComponent<Collectible>();
        Monologue monologue = GetComponent<Monologue>();

        if (monologue != null)
        {
            monologue.OnDialogueEndEvent += HandleMonologueEnd;
            monologue.OpenDialogOnTrigger();
            return; // Chờ cho đến khi nói xong
        }

        HandleCollection(item, collectible);
    }

    private void HandleMonologueEnd()
    {
        Monologue monologue = GetComponent<Monologue>();
        if (monologue != null)
            monologue.OnDialogueEndEvent -= HandleMonologueEnd;

        Item item = GetComponent<Item>();
        Collectible collectible = GetComponent<Collectible>();
        HandleCollection(item, collectible);
    }

    private void HandleCollection(Item item, Collectible collectible)
    {
        if (isCollected) return;

        bool itemAdded = inventoryController.AddItem(gameObject);
        if (!itemAdded)
        {
            Debug.Log("Inventory đầy, không thể nhặt " + item.Name);
            return;
        }

        isCollected = true;

        item.ShowPopUp();
        if (equipmentViewController != null)
            equipmentViewController.ShowEquipmentItems();

        if (collectible != null)
            collectible.OnPickedUp();

        if (saveController != null)
            saveController.SaveGame();

        Destroy(gameObject);
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
                TargetType.Item
            );
        }

        return new TargetInfoData("Item", null, "Nhặt", TargetType.Item);
    }
}
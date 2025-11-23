using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Gán vào Player để nhặt item khi chạm vào
public class PlayerItemCollector : MonoBehaviour
{
    private InventoryController inventoryController;
    private EquipmentScrollViewController equipmentViewController;

    void Start()
    {
        inventoryController = Object.FindFirstObjectByType<InventoryController>();
        equipmentViewController = Object.FindFirstObjectByType<EquipmentScrollViewController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Item")) return;
        if (PauseController.IsGamePause) return;

        Item item = collision.GetComponent<Item>();
        if (item == null) return;

        Collectible collectible = collision.GetComponent<Collectible>();
        Monologue monologue = collision.GetComponent<Monologue>();

        // === NẾU CÓ MONOLOGUE ===
        if (monologue != null)
        {
            void OnDialogueEnd()
            {
                // Sau khi nói xong mới add item
                bool added = inventoryController.AddItem(collision.gameObject);
                if (!added)
                {
                    Debug.Log("Inventory đầy, không thể nhặt " + item.Name);
                    monologue.OnDialogueEndEvent -= OnDialogueEnd;
                    return;
                }

                item.ShowPopUp();
                equipmentViewController.ShowEquipmentItems();

                if (collectible != null)
                    collectible.OnPickedUp();

                SaveController.Instance.TriggerAutoSave();
                Destroy(collision.gameObject);

                monologue.OnDialogueEndEvent -= OnDialogueEnd;
            }

            monologue.OnDialogueEndEvent += OnDialogueEnd;
            monologue.OpenDialogOnTrigger();
            return;
        }

        // === KHÔNG CÓ MONOLOGUE ===
        bool itemAdded = inventoryController.AddItem(collision.gameObject);
        if (!itemAdded)
        {
            Debug.Log("Inventory đầy, không thể nhặt " + item.Name);
            return;
        }

        item.ShowPopUp();
        equipmentViewController.ShowEquipmentItems();

        if (collectible != null)
            collectible.OnPickedUp();

        SaveController.Instance.TriggerAutoSave();
        Destroy(collision.gameObject);
    }

}
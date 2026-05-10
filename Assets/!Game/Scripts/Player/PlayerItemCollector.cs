using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{

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
                bool added = InventoryController.Instance.AddItem(item);
                if (!added)
                {
                    Debug.Log("Inventory đầy, không thể nhặt " + item.Name);
                    monologue.OnDialogueEndEvent -= OnDialogueEnd;
                    return;
                }

                item.ShowPopUp();

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
        bool itemAdded = InventoryController.Instance.AddItem(item);
        if (!itemAdded)
        {
            Debug.Log("Inventory đầy, không thể nhặt " + item.Name);
            return;
        }

        item.ShowPopUp();

        if (collectible != null)
            collectible.OnPickedUp();

        SaveController.Instance.TriggerAutoSave();
        Destroy(collision.gameObject);
    }
}
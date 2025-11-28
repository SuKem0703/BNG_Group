using System.Collections.Generic;
using UnityEngine;

public class StorageChest : MonoBehaviour, IInteractable
{
    [Header("Save Settings")]
    public string chestID;

    // Changed type to match what SaveController and StorageChestController expect
    // SaveController loads/stores InventorySaveData into chest.chestData at runtime.
    public List<InventorySaveData> chestData = new List<InventorySaveData>();

    void Awake()
    {
        if (string.IsNullOrEmpty(chestID))
        {
            chestID = GlobalHelper.GenerateUniqueID(gameObject);
        }
    }

    private void Start()
    {
        LoadChestData();
    }

    public void Interact()
    {
        StorageChestController.Instance.OpenChest(this);
    }

    private void LoadChestData()
    {
        // If you later implement local persistence per-chest, load here.
        // e.g. chestData = SaveController.Instance.LoadChest(chestID);

        if (chestData == null)
            chestData = new List<InventorySaveData>();
    }
    public void PlayOpenAnimation() { }
    public void PlayCloseAnimation() { }

    public bool CanInteract()
    {
        return GameStateManager.CanProcessInput();
    }
}
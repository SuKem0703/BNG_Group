using System.Collections.Generic;
using UnityEngine;

public class StorageChest : MonoBehaviour, IInteractable
{
    [Header("Save Settings")]
    public string chestID;

    public List<StorageChestSaveData> chestData = new List<StorageChestSaveData>();

    void Awake()
    {
        if (string.IsNullOrEmpty(chestID))
        {
            chestID = GlobalHelper.GenerateUniqueID(gameObject);
        }
    }

    private void Start()
    {
        if (chestData == null)
            chestData = new List<StorageChestSaveData>();
    }

    public void Interact()
    {
        StorageChestController.Instance.OpenChest(this);
    }

    public bool CanInteract()
    {
        return GameStateManager.CanProcessInput();
    }
}
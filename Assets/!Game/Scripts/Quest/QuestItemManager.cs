using System.Collections.Generic;
using UnityEngine;

public class QuestItemManager : MonoBehaviour
{
    [SerializeField] private List<Collectible> managedItems;

    [SerializeField] private List<Collectible> itemsToRemove = new List<Collectible>();

    void Awake()
    {
        managedItems = new List<Collectible>(
            GetComponentsInChildren<Collectible>(true)
        );
    }

    void Start()
    {
        if (SaveController.IsDataLoaded)
        {
            UpdateAllItems();
        }
        else
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void HandleDataLoaded()
    {
        UpdateAllItems();
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void OnEnable()
    {
        QuestController.OnQuestStatusUpdated += HandleQuestUpdate;
    }

    private void OnDisable()
    {
        QuestController.OnQuestStatusUpdated -= HandleQuestUpdate;
    }

    private void HandleQuestUpdate(string updatedQuestID)
    {
        itemsToRemove.Clear();
        foreach (var item in managedItems)
        {
            if (item == null)
            {
                itemsToRemove.Add(item);
                continue;
            }
            if (item.requiredQuestID == updatedQuestID)
            {
                item.UpdateVisibility();
            }
        }

        foreach (var item in itemsToRemove)
        {
            managedItems.Remove(item);
        }
    }

    private void UpdateAllItems()
    {
        itemsToRemove.Clear();

        foreach (var item in managedItems)
        {
            if (item == null)
            {
                itemsToRemove.Add(item);
                continue;
            }
            item.UpdateVisibility();
        }

        foreach (var item in itemsToRemove)
        {
            managedItems.Remove(item);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string currentSceneName;
    public string mapBoundary;

    public Vector3 checkpointPosition;
    public string checkpointSceneName;

    public int backPackSlotCount;
    public List<InventorySaveData> inventorySaveData;
    public List<InventorySaveData> hotbarSaveData;
    public List<ChestSaveData> chestSaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handInQuestIDs;

    public int currentKnightHP;
    public int currentmageHP;
    public int currentKnightMP;
    public int currentMageMP;
    public float currentStamina;

    public List<EquippedSaveData> knightEquipSaveData;
    public List<EquippedSaveData> mageEquipSaveData;
    public List<EquippedSaveData> shareEquipSaveData;

    public FarmData farmData;

    public List<ChestStorageEntry> allChestsData = new List<ChestStorageEntry>();

    public List<SaveController.SceneCollected> collectedByScene = new();
}
[System.Serializable]
public class ChestSaveData
{
    public string chestID;
    public bool isOpened;
}

[System.Serializable]
public class SceneCollected
{
    public string sceneName;
    public List<string> collectedIDs = new List<string>();
}

[System.Serializable]
public class ChestStorageEntry
{
    public string chestID;
    public List<StorageChestSaveData> items;
}
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
    public List<ChestSaveData> chestSaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handInQuestIDs;

    public int currentKnightHP;
    public int currentmageHP;
    public int currentKnightMP;
    public int currentMageMP;
    public float currentStamina;

    public FarmData farmData;

    public int currentDay;
    public float currentTimeOfDay;

    public List<ChestStorageEntry> allChestsData = new List<ChestStorageEntry>();

    public List<SaveController.SceneCollected> collectedByScene = new();

    public List<BestiaryEntry> bestiaryData = new List<BestiaryEntry>();
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

[System.Serializable]
public class BestiaryEntry
{
    public string enemyID;
    public int status;
    public int killCount;
}
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string currentSceneName;
    public int backPackSlotCount;
    public List<InventorySaveData> inventorySaveData;
    public List<InventorySaveData> hotbarSaveData;
    public List<ChestSaveData> chestSaveData;
    public List<QuestProgress> questProgressData;
    public List<string> handInQuestIDs;

    // Thêm các thông tin người chơi
    public int lvl;
    public int exp;
    public int coin;
    public int gem;

    public int currentKnightHP;
    public int currentmageHP;
    public int currentKnightMP;
    public int currentMageMP;
    public float currentStamina;

    // Thêm thông tin chỉ số và tiềm năng
    public int str;
    public int dex;
    public int con;
    public int intStat;
    public int potentialPoints;

    public List<EquippedSaveData> knightEquipSaveData;
    public List<EquippedSaveData> mageEquipSaveData;
    public List<EquippedSaveData> shareEquipSaveData;

    public FarmData farmData;

    // Persist collected items per scene
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
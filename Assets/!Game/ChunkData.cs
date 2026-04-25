using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
    Static,         // Trang trí (không có logic)
    Interactable,   // Monologue (Độc thoại, biển báo, tương tác 1 lần)
    NPC,            // NPC (Nhiều hội thoại, có choice)
    Enemy,          // Quái vật
    Container       // Rương, kho đồ
}

[System.Serializable]
public class EntitySaveData
{
    public string entityID;
    public EntityType entityType;
    public Vector3 position;

    public int sortingBuffer;

    // --- Dữ liệu cho Monologue ---
    public string monologueDataPath;
    public bool isOneTimeOnly;
    public string uniqueID;
    public bool triggerOnEnter;

    // --- Dữ liệu cho NPC ---
    public string[] npcDialoguePaths;

    // --- Dữ liệu cho Reward Chest ---
    public bool isOpened;
    public string rewardItemPath;
    public RarityMode rarityMode;
    public ItemRarity fixedRarity;
    public QualityFactorMode qualityMode;

    // --- Dữ liệu Collider Tương Tác ---
    public Vector2 triggerSize;
    public Vector2 triggerOffset;
}

[System.Serializable]
public class ChunkData
{
    public Vector2Int coord;
    public List<EntitySaveData> entities = new List<EntitySaveData>();
}
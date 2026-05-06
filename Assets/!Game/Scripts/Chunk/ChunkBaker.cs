using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class ChunkBaker : MonoBehaviour
{
    [Header("Cấu hình lưới")]
    public float chunkSize = 16f;
    [Header("Đầu ra")]
    public string savePath = "Assets/!Game/Resources/ChunkData/";

    [ContextMenu("Bake Map To Chunk Data")]
    public void BakeMap()
    {
        Dictionary<Vector2Int, ChunkData> mapData = new Dictionary<Vector2Int, ChunkData>();
        ChunkEntityMarker[] allEntities = Object.FindObjectsByType<ChunkEntityMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int count = 0;
        foreach (var marker in allEntities)
        {
            Vector2Int coord = WorldToGrid(marker.transform.position);

            if (!mapData.ContainsKey(coord)) mapData[coord] = new ChunkData { coord = coord };

            EntitySaveData data = new EntitySaveData
            {
                entityID = marker.prefabID,
                entityType = marker.entityType,
                position = marker.transform.position
            };

            var dynamicSort = marker.GetComponent<DynamicSorting>();
            if (dynamicSort != null)
            {
                data.sortingBuffer = dynamicSort.sortingBuffer;
            }
            else
            {
                var spriteSort = marker.GetComponent<SpriteDynamicSorting>();
                if (spriteSort != null)
                {
                    data.sortingBuffer = spriteSort.sortingBuffer;
                }
            }

            if (marker.entityType == EntityType.Interactable || marker.entityType == EntityType.NPC)
            {
                BoxCollider2D[] colliders = marker.GetComponents<BoxCollider2D>();
                foreach (var col in colliders)
                {
                    if (col.isTrigger)
                    {
                        data.triggerSize = col.size;
                        data.triggerOffset = col.offset;
                        break;
                    }
                }
            }

            if (marker.entityType == EntityType.Interactable)
            {
                var mono = marker.GetComponent<Monologue>();
                if (mono != null)
                {
                    data.triggerOnEnter = mono.triggerOnEnter;
                    data.isOneTimeOnly = mono.isOneTimeOnly;
                    data.uniqueID = string.IsNullOrEmpty(mono.uniqueID) ? marker.name + "_mono" : mono.uniqueID;
                    data.monologueDataPath = GetResourcePath(mono.monologueData);
                }
            }

            else if (marker.entityType == EntityType.NPC)
            {
                var npc = marker.GetComponent<NPC>();
                if (npc != null)
                {
                    data.triggerOnEnter = npc.triggerOnEnter;
                    if (npc.dialogueDataList != null)
                    {
                        data.npcDialoguePaths = new string[npc.dialogueDataList.Length];
                        for (int i = 0; i < npc.dialogueDataList.Length; i++)
                        {
                            data.npcDialoguePaths[i] = GetResourcePath(npc.dialogueDataList[i]);
                        }
                    }
                }
                var facing = marker.GetComponent<NPCAnimation>();
                if (facing != null)
                {
                    data.npcFacing = facing.initialFacing;
                }
            }

            else if (marker.entityType == EntityType.Container)
            {
                var chest = marker.GetComponent<Chest>();
                if (chest != null)
                {
                    data.rarityMode = chest.rarityMode;
                    data.fixedRarity = chest.fixedRarity;
                    data.qualityMode = chest.qualityMode;

                    if (chest.itemPrefab != null)
                    {
                        data.rewardItemPath = GetResourcePath(chest.itemPrefab);
                    }
                }
            }

            mapData[coord].entities.Add(data);
            count++;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string finalSavePath = Path.Combine(savePath, sceneName);

        if (!Directory.Exists(finalSavePath)) Directory.CreateDirectory(finalSavePath);

        foreach (var kvp in mapData)
        {
            string json = JsonUtility.ToJson(kvp.Value, true);
            string filePath = Path.Combine(finalSavePath, $"Chunk_{kvp.Key.x}_{kvp.Key.y}.json");
            File.WriteAllText(filePath, json);
            count++;
        }

        Debug.Log($"<color=green>[Bake Thành Công]</color> Scene '{sceneName}': {count} vật thể vào {mapData.Count} Chunks!");
    }

    private Vector2Int WorldToGrid(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSize);
        int y = Mathf.FloorToInt(pos.y / chunkSize);
        return new Vector2Int(x, y);
    }

    private string GetResourcePath(Object obj)
    {
        if (obj == null) return string.Empty;
#if UNITY_EDITOR
        string fullPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(fullPath)) return string.Empty;

        int resIndex = fullPath.IndexOf("Resources/");
        if (resIndex >= 0)
        {
            string pathWithoutRes = fullPath.Substring(resIndex + 10);

            int dotIndex = pathWithoutRes.LastIndexOf('.');
            if (dotIndex > 0)
            {
                return pathWithoutRes.Substring(0, dotIndex);
            }

            return pathWithoutRes;
        }
        Debug.LogWarning($"<color=orange>[Cảnh Báo]</color> Asset '{obj.name}' không nằm trong thư mục Resources! Hãy di chuyển nó vào thư mục Resources để game có thể Load ở Runtime.");
#endif
        return string.Empty;
    }
}
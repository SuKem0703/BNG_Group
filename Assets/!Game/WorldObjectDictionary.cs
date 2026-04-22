using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldObjectDictionary : MonoBehaviour
{
    [Header("Danh sách Prefab Decor/World Object")]
    public List<GameObject> worldPrefabs;

    private Dictionary<string, GameObject> prefabDict;

    private void Awake()
    {
        prefabDict = new Dictionary<string, GameObject>();
        foreach (var prefab in worldPrefabs)
        {
            if (prefab != null && !prefabDict.ContainsKey(prefab.name))
            {
                prefabDict.Add(prefab.name, prefab);
            }
        }
    }

    public GameObject GetPrefab(string prefabID)
    {
        if (prefabDict.TryGetValue(prefabID, out GameObject prefab))
        {
            return prefab;
        }
        Debug.LogWarning($"[WorldObjectDictionary] Không tìm thấy Prefab có ID: {prefabID}");
        return null;
    }

#if UNITY_EDITOR
    // Thêm Context Menu để tự động quét và nạp Prefab có chứa ChunkEntityMarker
    [ContextMenu("Auto Load All World Prefabs")]
    private void LoadAllWorldPrefabsFromProject()
    {
        worldPrefabs = new List<GameObject>();

        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (obj != null)
            {
                ChunkEntityMarker marker = obj.GetComponent<ChunkEntityMarker>();
                if (marker != null)
                {
                    worldPrefabs.Add(obj);
                }
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"<color=green>[Thành công]</color> Đã tự động nạp {worldPrefabs.Count} World Prefabs vào Dictionary!");
    }
#endif
}
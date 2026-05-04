using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemDictionary : MonoBehaviour
{
    public static ItemDictionary Instance { get; private set; }

    [Header("Danh sách tự động cập nhật")]
    public List<Item> itemPrefabs;

    private Dictionary<int, GameObject> itemDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        Instance = this;

        itemDictionary = new Dictionary<int, GameObject>();

        foreach (Item item in itemPrefabs)
        {
            if (item != null)
            {
                if (item.ID == 0) continue;

                if (itemDictionary.ContainsKey(item.ID))
                {
                    Debug.LogWarning($"[ItemDictionary] PHÁT HIỆN TRÙNG ID {item.ID} giữa '{item.Name}' và '{itemDictionary[item.ID].name}'");
                }
                else
                {
                    itemDictionary[item.ID] = item.gameObject;
                }
            }
        }
    }

    public GameObject GetItemPrefab(int itemID)
    {
        itemDictionary.TryGetValue(itemID, out GameObject prefab);
        if (prefab == null)
        {
            Debug.LogWarning($"Item with ID {itemID} not found in dictionary");
        }
        return prefab;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Load All Item Prefabs")]
    private void LoadAllItemsFromProject()
    {
        itemPrefabs = new List<Item>();

        // Tìm tất cả các file có định dạng là Prefab trong Project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (obj != null)
            {
                Item itemComp = obj.GetComponent<Item>();
                if (itemComp != null)
                {
                    itemPrefabs.Add(itemComp);
                }
            }
        }

        EditorUtility.SetDirty(this);

        Debug.Log($"<color=green>[Thành công]</color> Đã tự động nạp {itemPrefabs.Count} Item Prefabs vào Dictionary!");
    }
#endif
}
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyDictionary : MonoBehaviour
{
    public static EnemyDictionary Instance { get; private set; }

    [Header("Danh sách toàn bộ quái vật trong game")]
    public List<EnemyData> enemyDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }
        Instance = this;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Load All Enemy Data")]
    private void LoadAllEnemiesFromProject()
    {
        enemyDatabase = new List<EnemyData>();
        string[] guids = AssetDatabase.FindAssets("t:EnemyData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) enemyDatabase.Add(data);
        }
        EditorUtility.SetDirty(this);
        Debug.Log($"Đã tự động nạp {enemyDatabase.Count} EnemyData!");
    }
#endif
}
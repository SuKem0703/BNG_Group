#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabReconnector : EditorWindow
{
    private GameObject sourcePrefab;

    [MenuItem("Tools/Reconnect Prefabs")]
    public static void ShowWindow() => GetWindow<PrefabReconnector>("Reconnect");

    void OnGUI()
    {
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Target Root Prefab", sourcePrefab, typeof(GameObject), false);

        if (GUILayout.Button("Overwrite Selection to Target Root"))
        {
            foreach (var obj in Selection.objects)
            {
                if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
                {
                    string path = AssetDatabase.GetAssetPath(obj);

                    GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);

                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                    PrefabUtility.UnloadPrefabContents(prefabInstance);

                    Debug.Log($"Re-saved: {path}");
                }
            }
        }
    }
}
#endif
using UnityEditor;
using UnityEngine;

public class PrefabReverter : Editor
{
    [MenuItem("Tools/Prefab/Revert Selected Variants to Root")]
    public static void RevertVariants()
    {
        string[] selectedGuids = Selection.assetGUIDs;
        int count = 0;

        foreach (string guid in selectedGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (variantAsset != null && PrefabUtility.IsPartOfVariantPrefab(variantAsset))
            {
                GameObject rootAsset = PrefabUtility.GetCorrespondingObjectFromSource(variantAsset);

                if (rootAsset != null)
                {
                    GameObject tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(rootAsset);

                    PrefabUtility.SaveAsPrefabAssetAndConnect(tempInstance, path, InteractionMode.AutomatedAction);

                    DestroyImmediate(tempInstance);
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Reverter] Đã revert thành công {count} Variants về trạng thái Root.");
    }
}
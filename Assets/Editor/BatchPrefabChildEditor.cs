using UnityEngine;
using UnityEditor;

public class BatchPrefabChildEditor
{
    [MenuItem("Tools/Batch/Setup Behind Fade (Folder)")]
    static void SetupBehindFade()
    {
        Object[] selection = Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets);

        foreach (Object obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) continue;

            Transform behind = instance.transform.Find("Behind");
            if (behind == null)
            {
                Object.DestroyImmediate(instance);
                continue;
            }

            // Collider
            Collider2D col = behind.GetComponent<Collider2D>();
            if (col == null)
                col = behind.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Rigidbody
            Rigidbody2D rb = behind.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = behind.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // FadeTrigger
            if (behind.GetComponent<FadeTrigger>() == null)
                behind.gameObject.AddComponent<FadeTrigger>();

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Batch setup Behind complete");
    }
}

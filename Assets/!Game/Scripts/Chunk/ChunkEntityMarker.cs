using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ChunkEntityMarker : MonoBehaviour
{
    public string prefabID;
    public EntityType entityType = EntityType.Static;

    [ContextMenu("Force Update ID")]
    public void UpdateID()
    {
#if UNITY_EDITOR
        string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

        if (!string.IsNullOrEmpty(assetPath))
        {
            prefabID = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }
        else
        {
            var rootMarker = GetComponentInParent<ChunkEntityMarker>();

            if (rootMarker != null && rootMarker != this)
            {
                prefabID = rootMarker.prefabID + "_" + gameObject.name;
            }
            else
            {
                prefabID = gameObject.name.Replace("(Clone)", "").Trim();
            }
        }

        EditorUtility.SetDirty(this);
#endif
    }
}
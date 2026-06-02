using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MapBoundary : MonoBehaviour
{
    private static Dictionary<string, BoxCollider2D> boundaryRegistry = new Dictionary<string, BoxCollider2D>();

    [Tooltip("ID của Boundary, tự động lấy theo tên GameObject.")]
    public string boundaryID;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(boundaryID) && gameObject != null)
        {
            boundaryID = gameObject.name;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    [ContextMenu("Force Update ID")]
    public void UpdateID()
    {
        boundaryID = gameObject.name;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void Awake()
    {
        if (string.IsNullOrEmpty(boundaryID))
        {
            boundaryID = gameObject.name;
        }

        boundaryRegistry[boundaryID] = GetComponent<BoxCollider2D>();
    }

    private void OnDestroy()
    {
        if (boundaryRegistry.ContainsKey(boundaryID))
        {
            boundaryRegistry.Remove(boundaryID);
        }
    }

    public static BoxCollider2D GetBoundary(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (boundaryRegistry.TryGetValue(id, out BoxCollider2D col))
        {
            return col;
        }

        Debug.LogWarning($"[MapBoundary] Không tìm thấy Boundary nào có ID là: {id}");
        return null;
    }
}
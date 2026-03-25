using UnityEngine;

[ExecuteAlways]
public class AutoIDBehaviour : MonoBehaviour
{
    [field: SerializeField] public string UniqueID { get; protected set; }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (Application.isPlaying) return;

        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
        {
            UniqueID = "";
            return;
        }

        if (string.IsNullOrEmpty(UniqueID))
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) GenerateIDInEditor();
            };
        }
    }

    protected virtual void Update()
    {
        if (Application.isPlaying) return;

        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            if (transform.hasChanged)
            {
                GenerateIDInEditor();
                transform.hasChanged = false;
            }
        }
    }

    [ContextMenu("Force Generate ID")]
    protected void GenerateIDInEditor()
    {
        string newID = GlobalHelper.GenerateUniqueID(gameObject);

        if (UniqueID != newID)
        {
            UniqueID = newID;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
    }
#endif
}
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[InitializeOnLoad]
public class EditorOnlyHandler
{
    static EditorOnlyHandler()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Khi bắt đầu chạy (EnterPlayMode)
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            GameObject[] editorOnlyObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
            foreach (GameObject obj in editorOnlyObjects)
            {
                obj.SetActive(false);
            }
        }
        // Khi dừng chạy (EnteredEditMode)
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            GameObject[] editorOnlyObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
            foreach (GameObject obj in editorOnlyObjects)
            {
                obj.SetActive(true);
            }
        }
    }
}
#endif
using UnityEditor;
using UnityEngine;

public class BatchPivotSetter : EditorWindow
{
    private Vector2 newPivot = new Vector2(0.5f, 0f);  // bottom-center

    [MenuItem("Tools/Batch Pivot Setter (Safe Obsolete API)")]
    static void Open()
    {
        GetWindow<BatchPivotSetter>("Batch Pivot Setter");
    }

    void OnGUI()
    {
        GUILayout.Label("Set Pivot for Sliced Sprites", EditorStyles.boldLabel);

        newPivot = EditorGUILayout.Vector2Field("Pivot (0-1)", newPivot);

        if (GUILayout.Button("Apply Pivot"))
            Apply();
    }

    void Apply()
    {
        Object[] objs = Selection.objects;

        foreach (Object obj in objs)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.isReadable = true;

            // Lấy metadata cũ
#pragma warning disable 618
            var sheet = importer.spritesheet;
#pragma warning restore 618

            if (sheet == null || sheet.Length == 0)
            {
                Debug.LogWarning("⚠ Sprite không phải Multi hoặc chưa slice: " + path);
                continue;
            }

            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i].alignment = (int)SpriteAlignment.Custom;
                sheet[i].pivot = newPivot;
            }

#pragma warning disable 618
            importer.spritesheet = sheet;
#pragma warning restore 618

            importer.SaveAndReimport();

            Debug.Log("✔ Updated pivot: " + path);
        }
    }
}

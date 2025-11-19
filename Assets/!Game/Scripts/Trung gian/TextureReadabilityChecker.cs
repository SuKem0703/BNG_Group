#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TextureReadabilityChecker
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    [InitializeOnEnterPlayMode]
    static void CheckReadableTextures()
    {
        var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
        foreach (var tex in textures)
        {
            string path = AssetDatabase.GetAssetPath(tex);

            if (string.IsNullOrEmpty(path)) continue;

            if (path.StartsWith("Packages/")) continue;

            if (!path.StartsWith("Assets/")) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                Debug.LogWarning($"⚠️ Texture '{path}' is not readable! (Sprite outline/pixel read sẽ thất bại)", tex);
            }

        }
    }
}
#endif
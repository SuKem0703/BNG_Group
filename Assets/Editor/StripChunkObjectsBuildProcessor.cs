#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StripChunkObjectsBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (Application.isPlaying || report == null) return;

        ChunkEntityMarker[] markers = Object.FindObjectsByType<ChunkEntityMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int count = 0;
        foreach (var marker in markers)
        {
            Object.DestroyImmediate(marker.gameObject);
            count++;
        }

        if (count > 0)
        {
            Debug.Log($"<color=cyan>[Build Pre-Process]</color> Đã tự động dọn dẹp {count} vật thể Chunk Bake khỏi Scene '{scene.name}' để giảm dung lượng Build.");
        }
    }
}
#endif
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class AutoFillOverrideController
{
    // Thêm một mục vào menu chuột phải khi click vào asset
    [MenuItem("Assets/Auto Fill Animator Override")]
    public static void AutoFill()
    {
        // Lấy đối tượng đang được chọn
        var overrideController = Selection.activeObject as AnimatorOverrideController;
        if (overrideController == null) return;

        // Lấy đường dẫn của Override Controller hiện tại
        string path = AssetDatabase.GetAssetPath(overrideController);
        string folderPath = Path.GetDirectoryName(path);

        // Tìm tất cả các Animation Clip nằm trong cùng thư mục đó
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        List<AnimationClip> newClips = new List<AnimationClip>();
        
        foreach (string guid in guids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            newClips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath));
        }

        // Lấy danh sách các override hiện tại
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        // Đếm số lượng clip được gán thành công
        int matchCount = 0;

        // Duyệt qua danh sách và gán clip nếu trùng tên
        for (int i = 0; i < overrides.Count; i++)
        {
            var originalClip = overrides[i].Key;
            if (originalClip != null)
            {
                // Tìm clip trong thư mục có tên giống hệt clip gốc
                var matchingClip = newClips.FirstOrDefault(c => c.name == originalClip.name);
                
                if (matchingClip != null)
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, matchingClip);
                    matchCount++;
                }
            }
        }

        // Áp dụng thay đổi
        overrideController.ApplyOverrides(overrides);
        
        // Lưu lại các thay đổi để không bị mất khi tắt Unity
        EditorUtility.SetDirty(overrideController);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AutoFill] Đã tự động gán thành công {matchCount} clips vào {overrideController.name}");
    }

    // Hàm này đảm bảo menu "Auto Fill Animator Override" chỉ hiện lên khi bạn click vào một file Animator Override Controller
    [MenuItem("Assets/Auto Fill Animator Override", true)]
    public static bool AutoFillValidation()
    {
        return Selection.activeObject is AnimatorOverrideController;
    }
}
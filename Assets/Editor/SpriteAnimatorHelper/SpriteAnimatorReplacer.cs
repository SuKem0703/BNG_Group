using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SpriteAnimatorReplacer : EditorWindow
{
    private Texture2D newSpriteSheetTexture;
    private AnimationClip targetAnimationClip;
    private DefaultAsset targetFolder;

    [Header("Cấu hình linh hoạt tên (Pattern Replacement)")]
    private bool usePrefixReplacement = false;
    private string oldPrefix = "Swordsman_lvl1";
    private string newPrefix = "Swordsman_lvl2";

    private bool fallbackToFrameIndex = true;

    [MenuItem("Tools/Sprite Animator Replacer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteAnimatorReplacer>("Sprite Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Công cụ Ghi đè Sprite trong Animation theo Tên", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        newSpriteSheetTexture = EditorGUILayout.ObjectField("Sprite Sheet Mới (Texture)", newSpriteSheetTexture, typeof(Texture2D), false) as Texture2D;

        EditorGUILayout.Space();
        GUILayout.Label("Chọn chế độ thay thế:", EditorStyles.boldLabel);
        targetAnimationClip = EditorGUILayout.ObjectField("Animation Clip Đơn Lẻ", targetAnimationClip, typeof(AnimationClip), false) as AnimationClip;
        targetFolder = EditorGUILayout.ObjectField("Hoặc Thư Mục Chứa Animation", targetFolder, typeof(DefaultAsset), false) as DefaultAsset;

        EditorGUILayout.Space();
        GUILayout.Label("Tùy chọn Khớp Tên Nâng Cao:", EditorStyles.boldLabel);

        usePrefixReplacement = EditorGUILayout.Toggle("Đổi Tiền Tố Tên (Prefix)", usePrefixReplacement);
        if (usePrefixReplacement)
        {
            EditorGUI.indentLevel++;
            oldPrefix = EditorGUILayout.TextField("Tiền tố Cũ trong Anim:", oldPrefix);
            newPrefix = EditorGUILayout.TextField("Tiền tố Mới trong Sheet:", newPrefix);
            EditorGUI.indentLevel--;
        }

        fallbackToFrameIndex = EditorGUILayout.Toggle("Fallback theo Frame Index (_0, _1...)", fallbackToFrameIndex);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bắt Đầu Thay Thế Tự Động", GUILayout.Height(40)))
        {
            ReplaceSprites();
        }
    }

    private void ReplaceSprites()
    {
        if (newSpriteSheetTexture == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn Sprite Sheet mới!", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(newSpriteSheetTexture);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        Dictionary<string, Sprite> newSpritesByName = new Dictionary<string, Sprite>();
        List<Sprite> newSpritesList = new List<Sprite>();

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                if (!newSpritesByName.ContainsKey(sprite.name))
                {
                    newSpritesByName.Add(sprite.name, sprite);
                }
                newSpritesList.Add(sprite);
            }
        }

        if (newSpritesByName.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Sprite nào trong Texture này. Đã Slice Sprite Sheet chưa?", "OK");
            return;
        }

        List<AnimationClip> clipsToProcess = new List<AnimationClip>();
        if (targetAnimationClip != null)
        {
            clipsToProcess.Add(targetAnimationClip);
        }
        else if (targetFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
            foreach (string guid in guids)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip != null) clipsToProcess.Add(clip);
            }
        }

        if (clipsToProcess.Count == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn Animation Clip hoặc Thư mục chứa Clip!", "OK");
            return;
        }

        int totalReplaced = 0;
        foreach (var clip in clipsToProcess)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            bool clipModified = false;

            foreach (var binding in curveBindings)
            {
                if (binding.propertyName == "m_Sprite")
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                    for (int i = 0; i < keyframes.Length; i++)
                    {
                        Sprite oldSprite = keyframes[i].value as Sprite;
                        if (oldSprite != null)
                        {
                            string originalName = oldSprite.name;
                            Sprite matchedSprite = null;

                            // 1. Khớp theo tên tuyệt đối
                            if (newSpritesByName.TryGetValue(originalName, out matchedSprite))
                            {
                                // Match thành công
                            }
                            // 2. Khớp theo Prefix Replacement (VD: Swordsman_lvl1 -> Swordsman_lvl2)
                            else if (usePrefixReplacement && !string.IsNullOrEmpty(oldPrefix) && originalName.Contains(oldPrefix))
                            {
                                string targetName = originalName.Replace(oldPrefix, newPrefix);
                                newSpritesByName.TryGetValue(targetName, out matchedSprite);
                            }

                            // 3. Fallback: Khớp theo số thứ tự khung hình (Frame Index ở cuối tên)
                            if (matchedSprite == null && fallbackToFrameIndex)
                            {
                                int frameIndex = ExtractFrameIndex(originalName);
                                if (frameIndex >= 0 && frameIndex < newSpritesList.Count)
                                {
                                    matchedSprite = newSpritesList[frameIndex];
                                }
                            }

                            // Tiến hành thay thế nếu tìm thấy Sprite khớp
                            if (matchedSprite != null)
                            {
                                keyframes[i].value = matchedSprite;
                                totalReplaced++;
                                clipModified = true;
                            }
                        }
                    }

                    if (clipModified)
                    {
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                    }
                }
            }

            if (clipModified)
            {
                EditorUtility.SetDirty(clip);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Thành công!", $"Đã quét và thay thế thành công {totalReplaced} khung hình sprite trong {clipsToProcess.Count} Animation Clip!", "OK");
    }

    // Tách số thứ tự ở cuối tên file (Ví dụ: Swordsman_lvl1_attack_0 -> trả về 0)
    private int ExtractFrameIndex(string spriteName)
    {
        Match match = Regex.Match(spriteName, @"\d+$");
        if (match.Success && int.TryParse(match.Value, out int index))
        {
            return index;
        }
        return -1;
    }
}
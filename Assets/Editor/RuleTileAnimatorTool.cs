using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RuleTileAnimatorTool : EditorWindow
{
    private Texture2D spriteSheet;
    private DefaultAsset outputFolder;

    private int tilesPerVariant = 1;
    private float animationSpeed = 6f;

    private Vector2 scrollPosition;

    private const string DefaultOutputPath =
        "Assets/Generated/Water";

    [MenuItem("Tools/2D/Rule Tile Animator Tool")]
    private static void OpenWindow()
    {
        RuleTileAnimatorTool window =
            GetWindow<RuleTileAnimatorTool>();

        window.titleContent =
            new GUIContent("Rule Tile Animator");

        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition
        );

        EditorGUILayout.LabelField(
            "Rule Tile Animator Tool",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Tạo AnimatedTile từ Sprite đã cắt trong Sprite Editor.\n" +
            "Các frame được sắp xếp theo cột, từ trên xuống dưới.\n" +
            "Các cột liên tiếp được nhóm thành một biến thể.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
            "Sprite Sheet",
            spriteSheet,
            typeof(Texture2D),
            false
        );

        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Output Folder",
            outputFolder,
            typeof(DefaultAsset),
            false
        );

        tilesPerVariant = EditorGUILayout.IntPopup(
            "Tiles Per Variant",
            tilesPerVariant,
            new[] { "1 Tile", "2 Tiles" },
            new[] { 1, 2 }
        );

        animationSpeed = EditorGUILayout.Slider(
            "Animation Speed",
            animationSpeed,
            0.1f,
            30f
        );

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Ví dụ chiều rộng 2 tile:\n" +
            "Cột 1 = tile trái; cột 2 = tile phải.\n" +
            "Mỗi cột chứa các frame theo chiều dọc.\n" +
            "Mỗi cặp cột tạo thành một biến thể.",
            MessageType.None
        );

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(
                   spriteSheet == null))
        {
            if (GUILayout.Button(
                    "Analyze Sprite Sheet",
                    GUILayout.Height(32)))
            {
                AnalyzeSpriteSheet();
            }

            if (GUILayout.Button(
                    "Generate Animated Tiles",
                    GUILayout.Height(38)))
            {
                GenerateAnimatedTiles();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private List<Sprite> GetSprites()
    {
        if (spriteSheet == null)
            return new List<Sprite>();

        string path = AssetDatabase.GetAssetPath(spriteSheet);

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(sprite => sprite != null)
            .ToList();
    }

    private static List<List<Sprite>> GroupSpritesByColumn(
        List<Sprite> sprites)
    {
        var columns = new List<List<Sprite>>();

        if (sprites == null || sprites.Count == 0)
            return columns;

        // Gom các sprite có cùng tọa độ X vào một cột.
        // Dùng dung sai nhỏ để tránh sai số dấu phẩy động.
        const float tolerance = 0.01f;

        var orderedSprites = sprites
            .OrderBy(sprite => sprite.rect.x)
            .ThenByDescending(sprite => sprite.rect.y)
            .ThenBy(sprite => sprite.name)
            .ToList();

        foreach (Sprite sprite in orderedSprites)
        {
            float x = sprite.rect.x;

            List<Sprite> column = columns.FirstOrDefault(
                c => Mathf.Abs(c[0].rect.x - x) < tolerance
            );

            if (column == null)
            {
                column = new List<Sprite>();
                columns.Add(column);
            }

            column.Add(sprite);
        }

        // Đảm bảo thứ tự frame từ trên xuống dưới.
        foreach (List<Sprite> column in columns)
        {
            column.Sort((a, b) =>
            {
                int result = b.rect.y.CompareTo(a.rect.y);

                if (result != 0)
                    return result;

                return string.CompareOrdinal(
                    a.name,
                    b.name
                );
            });
        }

        return columns;
    }

    private void AnalyzeSpriteSheet()
    {
        List<Sprite> sprites = GetSprites();

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Không tìm thấy Sprite",
                "Hãy chọn texture đã được cắt thành các Sprite " +
                "trong Sprite Editor.",
                "OK"
            );

            return;
        }

        List<List<Sprite>> columns =
            GroupSpritesByColumn(sprites);

        int remainder = columns.Count % tilesPerVariant;

        string message =
            $"Tổng số Sprite: {sprites.Count}\n" +
            $"Số cột: {columns.Count}\n" +
            $"Số tile mỗi biến thể: {tilesPerVariant}\n";

        if (remainder != 0)
        {
            message +=
                "\nCẢNH BÁO: Số cột không chia hết cho " +
                "chiều rộng biến thể. Hãy kiểm tra Sprite Sheet.";
        }
        else
        {
            message +=
                $"Số biến thể dự kiến: " +
                $"{columns.Count / tilesPerVariant}\n";
        }

        int firstColumnFrameCount = columns[0].Count;

        message +=
            $"\nSố frame ở cột đầu tiên: " +
            $"{firstColumnFrameCount}";

        bool consistent = columns.All(
            column => column.Count == firstColumnFrameCount
        );

        if (!consistent)
        {
            message +=
                "\n\nCẢNH BÁO: Các cột có số frame khác nhau.";
        }

        EditorUtility.DisplayDialog(
            "Phân tích Sprite Sheet",
            message,
            "OK"
        );
    }

    private void GenerateAnimatedTiles()
    {
        List<Sprite> sprites = GetSprites();

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Lỗi",
                "Không tìm thấy Sprite để xử lý.",
                "OK"
            );

            return;
        }

        List<List<Sprite>> columns =
            GroupSpritesByColumn(sprites);

        if (columns.Count == 0)
            return;

        if (columns.Count % tilesPerVariant != 0)
        {
            EditorUtility.DisplayDialog(
                "Lỗi bố cục",
                "Số cột không chia hết cho chiều rộng biến thể.\n" +
                "Không thể tự động ghép các biến thể một cách an toàn.",
                "OK"
            );

            return;
        }

        int expectedFrameCount = columns[0].Count;

        bool consistent = columns.All(
            column => column.Count == expectedFrameCount
        );

        if (!consistent)
        {
            EditorUtility.DisplayDialog(
                "Lỗi frame",
                "Các cột có số frame không đồng nhất.\n" +
                "Hãy kiểm tra lại cách cắt Sprite Sheet.",
                "OK"
            );

            return;
        }

        string outputPath = GetOutputPath();

        EnsureFolderExists(outputPath);

        int variantCount = columns.Count / tilesPerVariant;
        int generatedCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int variant = 0;
                 variant < variantCount;
                 variant++)
            {
                for (int tile = 0;
                     tile < tilesPerVariant;
                     tile++)
                {
                    int columnIndex =
                        variant * tilesPerVariant + tile;

                    List<Sprite> frames =
                        columns[columnIndex];

                    var animatedTile =
                        ScriptableObject.CreateInstance<AnimatedTile>();

                    animatedTile.m_AnimatedSprites =
                        frames.ToArray();

                    animatedTile.m_MinSpeed = animationSpeed;
                    animatedTile.m_MaxSpeed = animationSpeed;

                    string tilePosition = tilesPerVariant == 1
                        ? "Tile_01"
                        : (tile == 0
                            ? "Tile_Left"
                            : "Tile_Right");

                    string assetName =
                        $"Water_Variant_{variant + 1:00}_{tilePosition}";

                    string assetPath =
                        $"{outputPath}/{assetName}.asset";

                    // Không ghi đè asset đã tồn tại.
                    if (AssetDatabase.LoadAssetAtPath<AnimatedTile>(
                            assetPath) != null)
                    {
                        DestroyImmediate(animatedTile);

                        throw new InvalidOperationException(
                            $"Asset đã tồn tại: {assetPath}\n" +
                            "Hãy chọn thư mục đầu ra khác hoặc " +
                            "đổi tên các asset hiện có."
                        );
                    }

                    AssetDatabase.CreateAsset(
                        animatedTile,
                        assetPath
                    );

                    generatedCount++;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "Không thể tạo Animated Tile",
                exception.Message,
                "OK"
            );
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "Hoàn tất",
            $"Đã tạo {generatedCount} Animated Tile.\n" +
            $"Số biến thể: {variantCount}\n" +
            $"Chiều rộng: {tilesPerVariant} tile\n" +
            $"Số frame mỗi tile: {expectedFrameCount}\n" +
            $"Tốc độ: {animationSpeed}\n\n" +
            $"Thư mục: {outputPath}",
            "OK"
        );
    }

    private string GetOutputPath()
    {
        if (outputFolder == null)
            return DefaultOutputPath;

        string path = AssetDatabase.GetAssetPath(outputFolder);

        if (AssetDatabase.IsValidFolder(path))
            return path;

        return DefaultOutputPath;
    }

    private static void EnsureFolderExists(string path)
    {
        string[] parts = path.Split('/');

        if (parts.Length == 0 || parts[0] != "Assets")
        {
            throw new InvalidOperationException(
                "Thư mục đầu ra phải nằm bên trong Assets."
            );
        }

        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    parts[i]
                );
            }

            currentPath = nextPath;
        }
    }
}
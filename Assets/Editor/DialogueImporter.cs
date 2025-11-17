using UnityEngine;
using UnityEditor; // Cần thiết cho các công cụ editor
using System.IO;   // Cần thiết để đọc file

public class DialogueImporter
{
    // Thêm một menu item vào menu Assets
    [MenuItem("Assets/Import NPC Dialogue from JSON")]
    private static void ImportDialogue()
    {
        // Lấy file JSON đang được chọn trong cửa sổ Project
        Object selectedObject = Selection.activeObject;
        if (selectedObject == null || !(selectedObject is TextAsset))
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn một file .json (TextAsset) trong cửa sổ Project.", "OK");
            return;
        }

        string jsonPath = AssetDatabase.GetAssetPath(selectedObject);

        // Đọc nội dung file JSON
        string jsonText = File.ReadAllText(jsonPath);

        try
        {
            // 1. Deserialize JSON vào lớp DTO (NPCDialogueData)
            NPCDialogueData data = JsonUtility.FromJson<NPCDialogueData>(jsonText);
            if (data == null)
            {
                throw new System.Exception("Không thể parse JSON. Kiểm tra lại cấu trúc file.");
            }

            // 2. Tạo một instance ScriptableObject (NPCDialogue) mới
            NPCDialogue dialogueSO = ScriptableObject.CreateInstance<NPCDialogue>();

            // 3. Map dữ liệu từ DTO sang ScriptableObject
            dialogueSO.npcName = data.npcName;
            dialogueSO.dialogueLines = data.dialogueLines;
            dialogueSO.autoProgressLines = data.autoProgressLines;
            dialogueSO.endDialogueLines = data.endDialogueLines;
            dialogueSO.autoProgressDelay = data.autoProgressDelay;
            dialogueSO.typingSpeed = data.typingSpeed;
            dialogueSO.questInProgressIndex = data.questInProgressIndex;
            dialogueSO.questCompletedIndex = data.questCompletedIndex;
            dialogueSO.noMoreQuestsIndex = data.noMoreQuestsIndex;

            // 4. Load các Asset tham chiếu từ đường dẫn
            if (!string.IsNullOrEmpty(data.npcPortraitPath))
            {
                dialogueSO.npcPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(data.npcPortraitPath);
                if (dialogueSO.npcPortrait == null)
                    Debug.LogWarning($"Không tìm thấy Sprite tại: {data.npcPortraitPath}");
            }

            if (!string.IsNullOrEmpty(data.questPath))
            {
                dialogueSO.quest = AssetDatabase.LoadAssetAtPath<Quest>(data.questPath);
                if (dialogueSO.quest == null)
                    Debug.LogWarning($"Không tìm thấy Quest tại: {data.questPath}");
            }

            // 5. Map mảng các choices
            if (data.choices != null)
            {
                dialogueSO.choices = new DialogueChoice[data.choices.Length];
                for (int i = 0; i < data.choices.Length; i++)
                {
                    DialogueChoiceData choiceData = data.choices[i];
                    DialogueChoice choiceSO = new DialogueChoice(); // Đây là class [Serializable] của bạn

                    choiceSO.dialogueIndex = choiceData.dialogueIndex;
                    choiceSO.choices = choiceData.choices;
                    choiceSO.nextDialogueIndexes = choiceData.nextDialogueIndexes;
                    choiceSO.endDialogues = choiceData.endDialogues;
                    choiceSO.specialActions = choiceData.specialActions;
                    choiceSO.specialTargetNames = choiceData.specialTargetNames;
                    choiceSO.giveQuest = choiceData.giveQuest;

                    dialogueSO.choices[i] = choiceSO;
                }
            }

            // 6. Lưu ScriptableObject thành file .asset
            // File .asset sẽ được tạo ngay bên cạnh file .json
            string assetPath = Path.ChangeExtension(jsonPath, ".asset");
            string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(dialogueSO, uniqueAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Import thành công! Đã tạo file: {uniqueAssetPath}");

            // Tự động chọn file vừa tạo để bạn thấy
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = dialogueSO;
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Lỗi Import", $"Đã xảy ra lỗi: {e.Message}", "OK");
            Debug.LogError($"Lỗi khi import dialogue: {e}");
        }
    }
}
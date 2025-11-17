using System;

// Các lớp DTO (Data Transfer Object) này dùng để mapping 1:1 với cấu trúc JSON.
// Chúng ta không dùng NPCDialogue trực tiếp vì JSON không thể chứa tham chiếu Sprite/Quest.

[Serializable]
public class NPCDialogueData
{
    public string npcName;
    public string npcPortraitPath; // Sẽ lưu đường dẫn, ví dụ: "Assets/Sprites/MyPortrait.png"
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public bool[] endDialogueLines;
    public float autoProgressDelay = 1.5f;
    public float typingSpeed = 0.05f;

    public DialogueChoiceData[] choices;

    public int questInProgressIndex;
    public int questCompletedIndex;
    public int noMoreQuestsIndex;
    public string questPath; // Sẽ lưu đường dẫn, ví dụ: "Assets/Quests/MainQuest01.asset"
}

[Serializable]
public class DialogueChoiceData
{
    public int dialogueIndex;
    public string[] choices;
    public int[] nextDialogueIndexes;
    public bool[] endDialogues;
    public SpecialActionType[] specialActions; // Giả sử SpecialActionType là một enum
    public string[] specialTargetNames;
    public bool[] giveQuest;

    // specialTargets là [NonSerialized] nên không cần đưa vào JSON
}
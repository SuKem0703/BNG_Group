using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDialogue", menuName = "NPC Dialogue")]
public class NPCDialogue : ScriptableObject
{
    public string npcName;
    public Sprite npcPortrait;
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public bool[] endDialogueLines;
    public float autoProgressDelay = 1.5f;
    public float typingSpeed = 0.05f;

    public DialogueChoice[] choices;

    public int questInProgressIndex;
    public int questCompletedIndex;
    public int noMoreQuestsIndex;
    public Quest quest;

    [Header("Trạng thái hiển thị (dành riêng cho Quest này)")]
    [Tooltip("Ẩn NPC khi Quest này ở trạng thái 'InProgress'")]
    public bool hideWhenInProgress = false;
    [Tooltip("Ẩn NPC khi Quest này ở trạng thái 'Completed' (chưa trả)")]
    public bool hideWhenCompleted = false;
    [Tooltip("Ẩn NPC khi Quest này ở trạng thái 'HandedIn' (đã trả)")]
    public bool hideWhenHandedIn = false;

    [Header("Tự động kích hoạt (dành riêng cho Quest này)")]
    [Tooltip("Tự kích hoạt hội thoại khi Quest này 'NotStarted'")]
    public bool triggerOnEnter_NotStarted = false;
    [Tooltip("Tự kích hoạt hội thoại khi Quest này 'InProgress'")]
    public bool triggerOnEnter_InProgress = false;
    [Tooltip("Tự kích hoạt hội thoại khi Quest này 'Completed'")]
    public bool triggerOnEnter_Completed = false;
    [Tooltip("Tự kích hoạt hội thoại khi Quest này 'NoMoreQuests' (đã trả)")]
    public bool triggerOnEnter_NoMoreQuests = false;

    [Header("Tự động nhận Quest")]
    [Tooltip("Tự động nhận Quest sau khi đọc hết dòng thoại cuối cùng (Không cần qua Choice)")]
    public bool autoGiveQuestOnEnd = false;
}

[System.Serializable]
public class DialogueChoice
{
    public int dialogueIndex;
    public string[] choices;
    public int[] nextDialogueIndexes;
    public bool[] endDialogues;
    public SpecialActionType[] specialActions;
    public string[] specialTargetNames;

    public bool[] giveQuest;

    [System.NonSerialized] public Object[] specialTargets;
}
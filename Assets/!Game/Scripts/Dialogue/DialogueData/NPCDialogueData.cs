using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "NPC Dialogue")]
public class NPCDialogueData : BaseDialogueData
{
    [Header("NPC Specific Info")]
    public string npcName;
    public Sprite npcPortrait;
    public DialogueChoice[] choices;

    [Header("Trạng thái hiển thị (Quest)")]
    public bool hideWhenNotStarted = false;
    public bool hideWhenInProgress = false;
    public bool hideWhenCompleted = false;
    public bool hideWhenHandedIn = false;

    [Header("Tự động kích hoạt (Quest)")]
    public bool triggerOnEnter_NotStarted = false;
    public bool triggerOnEnter_InProgress = false;
    public bool triggerOnEnter_Completed = false;
    public bool triggerOnEnter_NoMoreQuests = false;

    public bool autoGiveQuestOnEnd = false;

    public override DialogueChoice[] GetChoices() => choices;
    public override bool AutoGiveQuestOnEnd => autoGiveQuestOnEnd;
}
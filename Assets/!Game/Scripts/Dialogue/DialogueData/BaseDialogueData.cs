using UnityEngine;

public abstract class BaseDialogueData : ScriptableObject
{
    [Header("General Settings")]
    public float typingSpeed = 0.05f;
    public float autoProgressDelay = 1.5f;

    [Header("Content")]
    [TextArea(3, 10)]
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public bool[] endDialogueLines;

    [Header("Quest Condition Settings")]
    public Quest quest;
    public int questInProgressIndex;
    public int questCompletedIndex;
    public int noMoreQuestsIndex;

    public virtual DialogueChoice[] GetChoices() => new DialogueChoice[0];
    public virtual bool AutoGiveQuestOnEnd => false;
    public virtual bool HandleQuestAtEnd => false;
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
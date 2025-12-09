using UnityEngine;

[CreateAssetMenu(fileName = "NewMonologue", menuName = "Monologue")]
public class MonologueData : ScriptableObject
{
    [Header("General Settings")]
    public float typingSpeed = 0.05f;
    public float autoProgressDelay = 1.5f;

    [Header("Content")]
    [TextArea(3, 10)]
    public string[] dialogueLines;

    public bool[] autoProgressLines;

    public bool[] endDialogueLines;

    [Header("Quest Giving (Optional)")]
    public Quest quest;
    public bool triggerQuestAtEnd = false;
    public bool handleQuestAtEnd = false;

    [Header("Quest Condition Settings")]

    public int questInProgressIndex;

    public int questCompletedIndex;

    public int noMoreQuestsIndex;
}
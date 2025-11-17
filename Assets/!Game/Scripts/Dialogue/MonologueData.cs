using UnityEngine;

[CreateAssetMenu(fileName = "NewMonologue", menuName = "Dialogue/Monologue")]
public class MonologueData : ScriptableObject
{
    public float typingSpeed = 0.05f;

    [TextArea(3, 10)]
    public string[] dialogueLines;

    public bool[] autoProgressLines;
    public float autoProgressDelay = 1.5f;

    public Quest questToGive;
    public bool triggerQuestAtEnd = false;
}
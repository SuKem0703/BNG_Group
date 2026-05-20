using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Monologue")]
public class MonologueData : BaseDialogueData
{
    [Header("Monologue Specific")]
    public bool triggerQuestAtEnd = false;
    public bool handleQuestAtEnd = false;

    public override bool AutoGiveQuestOnEnd => triggerQuestAtEnd;
    public override bool HandleQuestAtEnd => handleQuestAtEnd;
}
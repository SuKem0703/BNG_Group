using SQLite;

[Table("Profiles")]
public class ProfileEntity
{
    [PrimaryKey]
    public string ProfileId { get; set; }
    public string PlayerName { get; set; }
    public long LastPlayed { get; set; }

    public string CurrentSceneName { get; set; }
    public float PlayerPosX { get; set; }
    public float PlayerPosY { get; set; }
    public float PlayerPosZ { get; set; }

    public string CheckpointSceneName { get; set; }
    public float CheckpointPosX { get; set; }
    public float CheckpointPosY { get; set; }
    public float CheckpointPosZ { get; set; }

    public int BackPackSlotCount { get; set; }
    public int CurrentKnightHP { get; set; }
    public int CurrentMageHP { get; set; }
    public int CurrentKnightMP { get; set; }
    public int CurrentMageMP { get; set; }
    public float CurrentStamina { get; set; }

    public int Coin { get; set; }
    public int Gem { get; set; }

    public string ChestSaveDataJson { get; set; }
    public string QuestProgressDataJson { get; set; }
    public string HandInQuestIDsJson { get; set; }
    public string CollectedBySceneJson { get; set; }
    public string FarmSaveDataJson { get; set; }
}
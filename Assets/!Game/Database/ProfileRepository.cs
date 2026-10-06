using System;
using System.Collections.Generic;
using SQLite;

public class ProfileRepository
{
    private SQLiteConnection db => DatabaseManager.Instance.GetConnection();

    public ProfileEntity CreateProfile(string playerName)
    {
        string newId = Guid.NewGuid().ToString();

        uint generatedMasterSeed = (uint)UnityEngine.Random.Range(100000, int.MaxValue);

        var newProfile = new ProfileEntity
        {
            ProfileId = newId,
            PlayerName = playerName,
            LastPlayed = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

            CurrentSceneName = "01_Home_Indoor",
            PlayerPosX = -8.0f,
            PlayerPosY = 7.0f,
            PlayerPosZ = 0.0f,

            CheckpointSceneName = "01_Home_Indoor",
            CheckpointPosX = -8.0f,
            CheckpointPosY = 7.0f,
            CheckpointPosZ = 0.0f,

            BackPackSlotCount = 35,
            CurrentKnightHP = 100,
            CurrentMageHP = 100,
            CurrentKnightMP = 50,
            CurrentMageMP = 50,
            CurrentStamina = 20.0f,

            Coin = 0,
            Gem = 0,

            ChestSaveDataJson = "[]",
            QuestProgressDataJson = "[]",
            HandInQuestIDsJson = "[]",
            CollectedBySceneJson = "[]",

            FarmSaveDataJson = "{\"plotDataList\":[]}"
        };

        db.Insert(newProfile);
        CreateDefaultEquippedWeapons(newId);
        return newProfile;
    }

    private void CreateDefaultEquippedWeapons(string profileId)
    {
        var defaultKnightSword = new InventoryItemEntity
        {
            ProfileId = profileId,
            ItemId = 101,
            Quantity = 1,
            SlotIndex = 2003,
            IsEquipped = true,
            Rarity = (int)ItemRarity.Common,
            QualityFactor = 0.5f,
            ChestId = null
        };
        db.Insert(defaultKnightSword);

        var defaultMageStaff = new InventoryItemEntity
        {
            ProfileId = profileId,
            ItemId = 201,
            Quantity = 1,
            SlotIndex = 2103,
            IsEquipped = true,
            Rarity = (int)ItemRarity.Common,
            QualityFactor = 0.5f,
            ChestId = null
        };
        db.Insert(defaultMageStaff);
    }

    public List<ProfileEntity> GetAllProfiles()
    {
        return db.Table<ProfileEntity>().OrderByDescending(p => p.LastPlayed).ToList();
    }

    public ProfileEntity GetProfile(string profileId)
    {
        return db.Table<ProfileEntity>().FirstOrDefault(p => p.ProfileId == profileId);
    }

    public void UpdateProfile(ProfileEntity profile)
    {
        profile.LastPlayed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        db.Update(profile);
    }
}
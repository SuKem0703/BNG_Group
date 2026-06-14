using System.Collections.Generic;
using UnityEngine;

public class SkillTreeService : MonoBehaviour
{
    public static SkillTreeService Instance { get; private set; }

    private Dictionary<string, int> unlockedSkills = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public int GetSkillRank(string skillID)
    {
        if (string.IsNullOrEmpty(skillID)) return 0;
        return unlockedSkills.ContainsKey(skillID) ? unlockedSkills[skillID] : 0;
    }

    public int GetAvailableSkillPoints()
    {
        int totalPoints = PlayerStats.Instance != null ? PlayerStats.Instance.level : 1;
        int usedPoints = 0;
        
        foreach (var rank in unlockedSkills.Values)
        {
            usedPoints += rank;
        }
        
        return totalPoints - usedPoints;
    }

    public bool CanUnlockOrUpgrade(SkillData skillData)
    {
        if (skillData == null) return false;
        
        if (GetAvailableSkillPoints() <= 0) return false;

        if (skillData.lvlReq > 0 && PlayerStats.Instance != null && PlayerStats.Instance.level < skillData.lvlReq)
            return false;

        if (skillData.skillReqToUnlock != null)
        {
            int reqRank = GetSkillRank(skillData.skillReqToUnlock.skillID);
            if (reqRank < skillData.skillRankReqToUnlock)
                return false;
        }

        return true;
    }

    public bool TryUpgradeSkill(SkillData skillData)
    {
        if (!CanUnlockOrUpgrade(skillData)) return false;

        string id = skillData.skillID;
        if (!unlockedSkills.ContainsKey(id))
        {
            unlockedSkills[id] = 1;
        }
        else if (unlockedSkills[id] < skillData.maxRank)
        {
            unlockedSkills[id]++;
        }
        else
        {
            return false;
        }
        
        if (SaveController.Instance != null)
        {
            SaveController.Instance.TriggerAutoSave();
        }
        
        return true;
    }

    public SkillSaveData GetSkillSaveData()
    {
        SkillSaveData data = new SkillSaveData();
        foreach (var kvp in unlockedSkills)
        {
            data.keys.Add(kvp.Key);
            data.values.Add(kvp.Value);
        }

        if (HotbarController.Instance != null)
        {
            var hotbarData = HotbarController.Instance.GetHotbarSkillsSaveData();
            foreach (var kvp in hotbarData)
            {
                data.hotbarSlots.Add(kvp.Key);
                data.hotbarSkillIDs.Add(kvp.Value);
            }
        }

        return data;
    }

    public void LoadSkillSaveData(SkillSaveData data)
    {
        unlockedSkills.Clear();
        if (data == null) return;
        
        for (int i = 0; i < data.keys.Count; i++)
        {
            unlockedSkills[data.keys[i]] = data.values[i];
        }

        if (HotbarController.Instance != null && SkillDictionary.Instance != null)
        {
            Dictionary<int, SkillData> loadedHotbarSkills = new Dictionary<int, SkillData>();
            for (int i = 0; i < data.hotbarSlots.Count; i++)
            {
                int slot = data.hotbarSlots[i];
                string skillId = data.hotbarSkillIDs[i];
                
                SkillData loadedData = SkillDictionary.Instance.GetSkill(skillId);
                if (loadedData != null)
                {
                    loadedHotbarSkills[slot] = loadedData;
                }
            }
            HotbarController.Instance.LoadHotbarSkillsSaveData(loadedHotbarSkills);
        }
    }
}
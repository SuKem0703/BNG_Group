using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkillDictionary : MonoBehaviour
{
    public static SkillDictionary Instance { get; private set; }

    [Header("Danh sách tự động cập nhật")]
    public List<SkillData> skillDataList;

    private Dictionary<string, SkillData> skillDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        skillDictionary = new Dictionary<string, SkillData>();

        foreach (SkillData data in skillDataList)
        {
            if (data != null)
            {
                if (string.IsNullOrEmpty(data.skillID)) continue;

                if (skillDictionary.ContainsKey(data.skillID))
                {
                    Debug.LogWarning($"[SkillDictionary] PHÁT HIỆN TRÙNG ID {data.skillID} giữa '{data.skillName}' và '{skillDictionary[data.skillID].skillName}'");
                }
                else
                {
                    skillDictionary[data.skillID] = data;
                }
            }
        }
    }

    public SkillData GetSkill(string skillID)
    {
        if (string.IsNullOrEmpty(skillID)) return null;
        
        skillDictionary.TryGetValue(skillID, out SkillData data);
        if (data == null)
        {
            Debug.LogWarning($"Skill with ID {skillID} not found in dictionary");
        }
        return data;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Load All Skill Data")]
    private void LoadAllSkillsFromProject()
    {
        skillDataList = new List<SkillData>();

        string[] guids = AssetDatabase.FindAssets("t:SkillData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);

            if (data != null)
            {
                skillDataList.Add(data);
            }
        }

        EditorUtility.SetDirty(this);

        Debug.Log($"<color=green>[Thành công]</color> Đã tự động nạp {skillDataList.Count} Skill Data vào Dictionary!");
    }
#endif
}
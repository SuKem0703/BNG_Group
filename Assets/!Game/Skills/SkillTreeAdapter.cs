using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class SkillTreeAdapter : MonoBehaviour
{
    public static SkillTreeAdapter Instance { get; private set; }

    public List<SkillUIItem> skillNodes;

    [Header("Skill Details Panel")]
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI levelReqText;
    public TextMeshProUGUI mpCostText;
    public TextMeshProUGUI skillReqText;
    public TextMeshProUGUI skillDescriptionText;
    public TextMeshProUGUI skillPointsText;
    
    [Header("Central Action")]
    public Button centralUpgradeButton;
    public Button centralEquipButton;

    private SkillData targetSkill;

    void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (centralUpgradeButton != null)
        {
            centralUpgradeButton.onClick.AddListener(OnCentralUpgradeButtonClicked);
        }

        if (centralEquipButton != null)
        {
            centralEquipButton.onClick.AddListener(OnCentralEquipButtonClicked);
        }
    }

    private void OnEnable()
    {
        SaveController.OnDataLoaded += RefreshAllNodes;
        RefreshAllNodes();
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= RefreshAllNodes;
    }

    public void OnSkillNodeHovered(SkillData data)
    {
        targetSkill = data;
        UpdateSkillDetailsPanel(targetSkill);
        RefreshButtonStates();
    }

    private void OnCentralUpgradeButtonClicked()
    {
        if (targetSkill == null) return;

        if (SkillTreeService.Instance.TryUpgradeSkill(targetSkill))
        {
            RefreshAllNodes();
            UpdateSkillDetailsPanel(targetSkill);
        }
    }

    private void OnCentralEquipButtonClicked()
    {
        if (targetSkill == null) return;
        
        if (HotbarController.Instance != null)
        {
            HotbarController.Instance.EnterAssignMode(targetSkill);
        }
    }

    public void RefreshAllNodes()
    {
        if (skillPointsText != null)
        {
            skillPointsText.text = $"Còn lại: {SkillTreeService.Instance.GetAvailableSkillPoints()}";
        }

        foreach (var node in skillNodes)
        {
            if (node.skillData == null) continue;
            int currentRank = SkillTreeService.Instance.GetSkillRank(node.skillData.skillID);
            bool canUnlock = SkillTreeService.Instance.CanUnlockOrUpgrade(node.skillData);
            node.UpdateUI(currentRank, canUnlock);
        }

        RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        if (centralUpgradeButton != null)
        {
            if (targetSkill == null)
            {
                centralUpgradeButton.interactable = false;
            }
            else
            {
                bool canUpgrade = SkillTreeService.Instance.CanUnlockOrUpgrade(targetSkill);
                int currentRank = SkillTreeService.Instance.GetSkillRank(targetSkill.skillID);
                centralUpgradeButton.interactable = canUpgrade && currentRank < targetSkill.maxRank;
            }
        }

        if (centralEquipButton != null)
        {
            if (targetSkill == null)
            {
                centralEquipButton.gameObject.SetActive(false);
            }
            else
            {
                int currentRank = SkillTreeService.Instance.GetSkillRank(targetSkill.skillID);
                bool isUnlockedActiveSkill = (targetSkill.skillType == SkillType.Active) && (currentRank > 0);
                centralEquipButton.gameObject.SetActive(isUnlockedActiveSkill);
            }
        }
    }

    private void UpdateSkillDetailsPanel(SkillData data)
    {
        if (data == null) return;

        int rank = SkillTreeService.Instance.GetSkillRank(data.skillID);
        
        skillNameText.text = $"{data.skillName} [{rank}/{data.maxRank}]";

        string typePrefix = "";
        switch (data.skillType)
        {
            case SkillType.Modifier: typePrefix = "[Cường hóa]"; break;
            case SkillType.Active: typePrefix = "[Chủ động]"; break;
            case SkillType.Passive: typePrefix = "[Bị động]"; break;
        }
        skillDescriptionText.text = $"{typePrefix} {data.description}";
        
        levelReqText.text = data.lvlReq > 0 ? $"[Yêu cầu: Lvl.{data.lvlReq}]" : "";
        
        skillReqText.text = data.skillReqToUnlock != null ? $"[Thành thạo: {data.skillReqToUnlock.skillName} Lvl.{data.skillRankReqToUnlock}]" : "";

        mpCostText.text = $"MP: {data.GetManaCost(rank)}";
    }
}
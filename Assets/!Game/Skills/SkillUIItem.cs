using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkillUIItem : MonoBehaviour, IPointerEnterHandler
{
    public SkillData skillData;
    public Image skillIcon;
    public TextMeshProUGUI levelText;
    public GameObject lockOverlay;

    public void UpdateUI(int currentRank, bool canUnlock)
    {
        if (skillData == null) return;

        if (skillIcon != null) 
            skillIcon.sprite = skillData.skillIcon;

        if (levelText != null) 
            levelText.text = currentRank.ToString();

        if (lockOverlay != null)
            lockOverlay.SetActive(currentRank == 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SkillTreeAdapter.Instance != null && skillData != null)
        {
            SkillTreeAdapter.Instance.OnSkillNodeHovered(skillData);
        }
    }
}
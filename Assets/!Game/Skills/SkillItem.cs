using UnityEngine;
using UnityEngine.UI;

public class SkillItem : MonoBehaviour
{
    public string skillID;
    public Image icon;

    public void Setup(SkillData data)
    {
        skillID = data.skillID;
        if (icon != null)
        {
            icon.sprite = data.skillIcon;
        }
    }
}
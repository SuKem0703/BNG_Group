using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MPUI : MonoBehaviour
{
    public PlayerStats playerStats;
    public Image mpBarFill;
    public TextMeshProUGUI mpText;

    void Update()
    {
        if (playerStats != null)
        {
            int maxKnightMP = playerStats.finalKnightMaxMP; // Max MP hiệp sĩ
            int currentKnightMP = playerStats.knightMP;

            int maxMageMP = playerStats.finalMageMaxMP;     // Max MP pháp sư
            int currentMageMP = playerStats.mageMP;

            // Ví dụ: hiển thị MP của Knight
            float fillAmount = (float)currentKnightMP / maxKnightMP;
            mpBarFill.fillAmount = fillAmount;

            mpText.text = currentKnightMP + " / " + maxKnightMP;
        }
    }
}

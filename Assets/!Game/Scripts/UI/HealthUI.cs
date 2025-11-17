using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthUI : MonoBehaviour
{
    public PlayerStats playerStats;
    public Image healthBarFill;
    public TextMeshProUGUI healthText;

    void Update()
    {
        if (playerStats != null)
        {
            int maxKnightHealth = playerStats.finalKnightMaxHP; // Sử dụng finalMaxHP thay vì GetMaxHP()
            int currentKnightHealth = playerStats.knightHealth;

            int maxMageHealth = playerStats.finalMageMaxHP;
            int currentMageHealth = playerStats.mageHealth;

            float fillAmount = (float)currentKnightHealth / maxKnightHealth;
            healthBarFill.fillAmount = fillAmount;

            healthText.text = currentKnightHealth + " / " + maxKnightHealth;
        }
    }
}

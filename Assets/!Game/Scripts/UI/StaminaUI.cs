using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaUI : MonoBehaviour
{
    public PlayerStats playerStamina;
    public Image staminaBarFill;
    public TextMeshProUGUI staminaText;

    void Update()
    {
        if (playerStamina != null)
        {
            float maxStamina = playerStamina.finalStamina;
            float currentStamina = playerStamina.currentStamina;

            float fillAmount = (float)currentStamina / maxStamina;
            staminaBarFill.fillAmount = fillAmount;

            staminaText.text = currentStamina + " / " + maxStamina;
        }
    }
}

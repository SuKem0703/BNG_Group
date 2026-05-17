using TMPro;
using UnityEngine;

public class ItemQuantityDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text quantityTextOnUI;
    [SerializeField] private TMP_Text quantityTextOnWorld;

    private void Awake()
    {
        if (quantityTextOnUI == null || quantityTextOnWorld == null)
        {
            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in allTexts)
            {
                if (text.name == "QuantityTextOnUI") quantityTextOnUI = text;
                else if (text.name == "QuantityTextOnWorld") quantityTextOnWorld = text;
            }
        }
    }

    public void UpdateDisplay(string displayText)
    {
        if (quantityTextOnUI != null) quantityTextOnUI.text = displayText;
        if (quantityTextOnWorld != null) quantityTextOnWorld.text = displayText;
    }
}
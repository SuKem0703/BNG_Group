using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Key tương ứng trong file JSON (VD: MAIN_START)")]
    public string key;

    private TextMeshProUGUI textComponent;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        UpdateText();
        LocalizationManager.OnLanguageChanged += UpdateText;
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (LocalizationManager.Instance == null) return;
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(key)) return;

        textComponent.text = LocalizationManager.Instance.GetText(key);
    }
}
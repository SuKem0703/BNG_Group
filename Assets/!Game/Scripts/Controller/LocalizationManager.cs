using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event System.Action OnLanguageChanged;

    private Dictionary<string, string> localizedText;
    public string CurrentLang { get; private set; } = "vi";
    private bool isReady = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        string savedLang = PlayerPrefs.GetString("Language", "vi");
        LoadLanguage(savedLang);
    }

    public void LoadLanguage(string langCode)
    {
        CurrentLang = langCode;
        localizedText = new Dictionary<string, string>();

        TextAsset targetFile = Resources.Load<TextAsset>($"Localization/{langCode}");

        if (targetFile != null)
        {
            // Parse JSON
            LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(targetFile.text);

            foreach (var item in loadedData.items)
            {
                if (!localizedText.ContainsKey(item.key))
                {
                    localizedText.Add(item.key, item.value);
                }
            }
            Debug.Log($"[Localization] Đã load ngôn ngữ: {langCode}");
        }
        else
        {
            Debug.LogError($"[Localization] Không tìm thấy file ngôn ngữ: Localization/{langCode}");
        }

        isReady = true;
        PlayerPrefs.SetString("Language", CurrentLang);
        OnLanguageChanged?.Invoke();
    }

    public string GetText(string key)
    {
        if (!isReady) return key;

        if (localizedText.ContainsKey(key))
        {
            return localizedText[key];
        }

        return key;
    }

    public void ToggleLanguage()
    {
        string newLang = (CurrentLang == "vi") ? "en" : "vi";
        LoadLanguage(newLang);
    }
}

// Class đệm để đọc JSON của Unity
[System.Serializable]
public class LocalizationData
{
    public LocalizationItem[] items;
}

[System.Serializable]
public class LocalizationItem
{
    public string key;
    public string value;
}
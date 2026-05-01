using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event System.Action OnLanguageChanged;

    private Dictionary<string, string> localizedText;
    public string CurrentLang { get; private set; } = "vi";
    private bool isReady = false;

    private string saveFilePath;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        Instance = this;

        saveFilePath = Application.persistentDataPath + "/game_settings.json";

        string savedLang = "vi";
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                SaveSetting data = JsonUtility.FromJson<SaveSetting>(json);
                if (data != null && !string.IsNullOrEmpty(data.language))
                {
                    savedLang = data.language;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Lỗi đọc ngôn ngữ từ file: {ex.Message}");
            }
        }

        LoadLanguage(savedLang);
    }

    public void LoadLanguage(string langCode)
    {
        CurrentLang = langCode;
        localizedText = new Dictionary<string, string>();

        TextAsset targetFile = Resources.Load<TextAsset>($"Localization/{langCode}");

        if (targetFile != null)
        {
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

        SaveLanguageToDisk(CurrentLang);

        OnLanguageChanged?.Invoke();
    }

    // Hàm phụ trợ để lưu riêng ngôn ngữ mà không cần gọi SaveSettingController
    private void SaveLanguageToDisk(string lang)
    {
        SaveSetting data = new SaveSetting();

        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                data = JsonUtility.FromJson<SaveSetting>(json);
            }
            catch { }
        }

        data.language = lang;

        try
        {
            File.WriteAllText(saveFilePath, JsonUtility.ToJson(data, true));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Không thể lưu ngôn ngữ: {ex.Message}");
        }
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
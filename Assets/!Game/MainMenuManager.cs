using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button newGameMenuButton;
    public Button continueMenuButton;

    [Header("UI Panels")]
    public GameObject newGamePanel;
    public GameObject loadPanel;

    [Header("New Game UI")]
    public TMP_InputField playerNameInput;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Load Character UI")]
    public TextMeshProUGUI selectedCharacterNameText;
    public Button prevCharacterButton;
    public Button nextCharacterButton;
    public Button playSelectedCharacterButton;
    public Button closeLoadPanelButton;

    private List<ProfileEntity> loadedProfiles = new List<ProfileEntity>();
    private int currentProfileIndex = 0;

    private ProfileRepository profileRepo;

    private void Start()
    {
        profileRepo = new ProfileRepository();

        newGameMenuButton.onClick.AddListener(OpenNewGamePanel);
        continueMenuButton.onClick.AddListener(OpenLoadPanel);
        confirmButton.onClick.AddListener(OnConfirmNewGame);
        cancelButton.onClick.AddListener(OnCancelNewGame);

        prevCharacterButton.onClick.AddListener(OnPrevCharacter);
        nextCharacterButton.onClick.AddListener(OnNextCharacter);
        playSelectedCharacterButton.onClick.AddListener(OnPlaySelectedCharacter);

        if (closeLoadPanelButton != null)
            closeLoadPanelButton.onClick.AddListener(CloseLoadPanel);

        RefreshMainMenuButtons();

        newGamePanel.SetActive(false);
        if (loadPanel != null) loadPanel.SetActive(false);
    }

    private void RefreshMainMenuButtons()
    {
        var profiles = profileRepo.GetAllProfiles();

        continueMenuButton.gameObject.SetActive(profiles.Count > 0);
        newGameMenuButton.interactable = profiles.Count < 3;
    }

    public void OpenNewGamePanel()
    {
        var profiles = profileRepo.GetAllProfiles();
        if (profiles.Count >= 3)
        {
            Debug.LogWarning("Chỉ được tạo tối đa 3 nhân vật!");
            return;
        }

        playerNameInput.text = "";
        newGamePanel.SetActive(true);
    }

    private void OnCancelNewGame()
    {
        newGamePanel.SetActive(false);
    }

    private bool ValidatePlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Tên nhân vật không được để trống!");
            return false;
        }

        if (name.Length < 3 || name.Length > 16)
        {
            Debug.LogWarning("Tên nhân vật phải từ 3 đến 16 ký tự!");
            return false;
        }

        if (!Regex.IsMatch(name, @"^[\p{L}0-9 _-]+$"))
        {
            Debug.LogWarning("Tên chỉ được chứa chữ cái, số, khoảng trắng, gạch ngang (-) hoặc gạch dưới (_)!");
            return false;
        }

        return true;
    }

    private void OnConfirmNewGame()
    {
        string pName = playerNameInput.text.Trim();

        if (!ValidatePlayerName(pName))
        {
            return;
        }

        ProfileEntity newProfile = profileRepo.CreateProfile(pName);
        Debug.Log($"Đã tạo nhân vật: {newProfile.PlayerName} (ID: {newProfile.ProfileId})");

        PlayerPrefs.SetString("CurrentProfileId", newProfile.ProfileId);
        PlayerPrefs.Save();

        StartCoroutine(LoadGameScene(newProfile.CurrentSceneName));
    }

    public void OpenLoadPanel()
    {
        loadedProfiles = profileRepo.GetAllProfiles();
        if (loadedProfiles.Count == 0) return;

        currentProfileIndex = 0;
        UpdateProfileDisplay();

        loadPanel.SetActive(true);
    }

    private void CloseLoadPanel()
    {
        loadPanel.SetActive(false);
    }

    private void OnPrevCharacter()
    {
        if (loadedProfiles.Count <= 1) return;

        currentProfileIndex--;
        if (currentProfileIndex < 0)
        {
            currentProfileIndex = loadedProfiles.Count - 1;
        }
        UpdateProfileDisplay();
    }

    private void OnNextCharacter()
    {
        if (loadedProfiles.Count <= 1) return;

        currentProfileIndex++;
        if (currentProfileIndex >= loadedProfiles.Count)
        {
            currentProfileIndex = 0;
        }
        UpdateProfileDisplay();
    }

    private void UpdateProfileDisplay()
    {
        if (loadedProfiles.Count == 0) return;

        selectedCharacterNameText.text = loadedProfiles[currentProfileIndex].PlayerName;

        bool hasMultipleCharacters = loadedProfiles.Count > 1;
        prevCharacterButton.gameObject.SetActive(hasMultipleCharacters);
        nextCharacterButton.gameObject.SetActive(hasMultipleCharacters);
    }

    private void OnPlaySelectedCharacter()
    {
        if (loadedProfiles.Count == 0) return;

        ProfileEntity selectedProfile = loadedProfiles[currentProfileIndex];
        PlayerPrefs.SetString("CurrentProfileId", selectedProfile.ProfileId);
        PlayerPrefs.Save();

        StartCoroutine(LoadGameScene(selectedProfile.CurrentSceneName));
    }

    private IEnumerator LoadGameScene(string sceneName)
    {
        newGamePanel.SetActive(false);
        if (loadPanel != null) loadPanel.SetActive(false);

        yield return null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}
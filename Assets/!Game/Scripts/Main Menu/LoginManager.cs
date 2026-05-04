using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [SerializeField] private GameObject loginUI;
    [SerializeField] private TextMeshProUGUI uidText;

    [SerializeField] private GameObject buttonLogin;
    [SerializeField] private GameObject buttonLogout;

    [SerializeField] MiniLoadingScreen miniLoadingScreen;

    void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu" && miniLoadingScreen == null)
        {
            miniLoadingScreen = FindFirstObjectByType<MiniLoadingScreen>();
            if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        loginUI.SetActive(false);
        CheckLoginStatus();
        UpdateLoginUI();

        // Đăng ký sự kiện đổi ngôn ngữ để cập nhật lại UID text nếu đang ở menu
        LocalizationManager.OnLanguageChanged += CheckLoginStatus;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= CheckLoginStatus;
    }

    // Hàm kiểm tra và hiển thị UID
    private void CheckLoginStatus()
    {
        string token = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(token))
        {
            string accountId = PlayerPrefs.GetString("AccountId", "");
            if (!string.IsNullOrEmpty(accountId) && uidText != null)
            {
                // [LOCALIZATION] Dùng string.Format để điền ID vào chuỗi "UID: {0}"
                string format = GetText("UI_UID");
                uidText.text = string.Format(format, accountId);
            }
        }
        else
        {
            if (uidText != null)
                uidText.text = GetText("UI_NOT_LOGGED_IN");
        }
    }

    public void OnRegisterButtonPressed()
    {
        // [NETWORK] Dùng helper lấy URL đăng ký
        Application.OpenURL(NetworkConfig.GetUrl("Accounts/Create"));
    }

    public void OnLoginButtonPressed()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = GetText("MSG_INPUT_EMPTY");
            return;
        }

        StartCoroutine(Login(username, password));
    }

    public void OnPlayButtonPressed()
    {
        StartCoroutine(CheckServerConnection((isOnline) =>
        {
            if (!isOnline)
            {
                feedbackText.text = GetText("MSG_SERVER_ERROR");
                return;
            }

            string token = PlayerPrefs.GetString("AuthToken", "");
            if (!string.IsNullOrEmpty(token))
            {
                if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(true);
                StartCoroutine(FetchSaveData());
            }
            else
            {
                loginUI.SetActive(true);
                feedbackText.text = GetText("MSG_LOGIN_REQUIRED");
            }
        }));
    }

    IEnumerator CheckServerConnection(System.Action<bool> callback)
    {
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(true);

        // [NETWORK] URL Ping
        string url = NetworkConfig.GetUrl("api/GameData/ping");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool serverUp = request.result == UnityWebRequest.Result.Success;
            callback?.Invoke(serverUp);
        }

        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(false);
    }

    IEnumerator Login(string username, string password)
    {
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(true);

        var requestData = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(requestData);

        // [NETWORK] URL Login
        string url = NetworkConfig.GetUrl("api/Accounts/login");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            LoginResponse res = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            PlayerPrefs.SetString("AuthToken", res.token);
            PlayerPrefs.SetString("AccountId", res.accountId);
            PlayerPrefs.SetString("Username", res.username);
            PlayerPrefs.SetString("Role", res.role);

            Debug.Log($"Đăng nhập thành công! ID: {res.accountId}");

            HideLoginPanel();
            CheckLoginStatus();
            UpdateLoginUI();
        }
        else if (request.responseCode == 403)
        {
            feedbackText.text = GetText("MSG_ACCOUNT_LOCKED");
            Debug.LogWarning("Tài khoản bị khóa.");
        }
        else
        {
            feedbackText.text = GetText("MSG_LOGIN_FAILED");
            Debug.LogError("Lỗi đăng nhập: " + request.downloadHandler.text);
        }

        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(false);
    }

    IEnumerator FetchSaveData()
    {
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(true);

        string token = PlayerPrefs.GetString("AuthToken", "");
        if (string.IsNullOrEmpty(token))
        {
            yield break;
        }

        string url = NetworkConfig.GetUrl("api/GameData/get-save");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string saveJson = request.downloadHandler.text;
            SaveData saveData = JsonUtility.FromJson<SaveData>(saveJson);

            string targetScene = !string.IsNullOrEmpty(saveData.currentSceneName) ? saveData.currentSceneName : "MAP_CH1_01";

            SaveController.pendingSceneName = targetScene;
            SaveController.nextSpawnPosition = saveData.playerPosition;

            if (NetworkManager.Singleton != null)
            {
                var portConfig = NetworkManager.Singleton.GetComponent<NetworkPortConfig>();
                if (portConfig != null)
                {
                    portConfig.StartHostWithDynamicPort();
                }
                else
                {
                    Debug.LogWarning("[LoginManager] Không tìm thấy NetworkPortConfig, đang dùng Port mặc định!");
                    NetworkManager.Singleton.StartHost();
                }

                yield return null;

                NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }
        }
        else
        {
            Logout();
            loginUI.SetActive(true);
            feedbackText.text = GetText("MSG_SESSION_EXPIRED");
        }

        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(false);
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("AccountId");
        PlayerPrefs.Save();

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            NetworkManager.Singleton.Shutdown();
        }

        CheckLoginStatus();
        UpdateLoginUI();
        HideLoginPanel();
    }

    private void UpdateLoginUI()
    {
        string token = PlayerPrefs.GetString("AuthToken", "");
        bool isLoggedIn = !string.IsNullOrEmpty(token);

        buttonLogin.SetActive(!isLoggedIn);
        buttonLogout.SetActive(isLoggedIn);
    }

    public void ShowLoginPanel()
    {
        if (loginUI != null)
        {
            loginUI.SetActive(true);
            usernameInput.text = PlayerPrefs.GetString("Username", "");
            passwordInput.text = "";
            feedbackText.text = "";
        }
    }

    public void HideLoginPanel()
    {
        if (loginUI != null)
            loginUI.SetActive(false);
    }

    private string GetText(string key)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(key);
        return key;
    }

    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string token;
        public string accountId;
        public string username;
        public string role;
    }
}
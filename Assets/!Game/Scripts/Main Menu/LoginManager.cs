using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

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

    private string loginUrl = "https://chronicles-of-knight-and-mage.onrender.com/api/GameData/login";
    void Start()
    {
        loginUI.SetActive(false);

        string token = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(token))
        {
            Debug.Log("Đã có token, giữ nguyên để người chơi bấm Play.");
            string accountId = PlayerPrefs.GetString("AccountId", "");

            if (!string.IsNullOrEmpty(accountId) && uidText != null)
                uidText.text = $"UID: {accountId}";
        }
        else
        {
            uidText.text = "UID: Chưa đăng nhập";
            Debug.Log("Chưa có token. Vui lòng đăng nhập.");
        }
        UpdateLoginUI();

    }
    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu" && miniLoadingScreen == null)
        {
            miniLoadingScreen = FindFirstObjectByType<MiniLoadingScreen>();
            miniLoadingScreen.gameObject.SetActive(false);
        }
    }
    public void OnLoginButtonPressed()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            feedbackText.text = "Vui lòng nhập tên đăng nhập và mật khẩu.";
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
                feedbackText.text = "Không thể kết nối đến máy chủ.";
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
                feedbackText.text = "Bạn cần đăng nhập trước khi chơi.";
            }
        }));
    }
    IEnumerator CheckServerConnection(System.Action<bool> callback)
    {
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(true);

        using (UnityWebRequest request = UnityWebRequest.Get("https://chronicles-of-knight-and-mage.onrender.com/api/GameData/ping"))
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

        UnityWebRequest request = new UnityWebRequest(loginUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            LoginResponse res = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            PlayerPrefs.SetString("AccountId", res.id);
            PlayerPrefs.SetString("Username", res.username);
            PlayerPrefs.SetString("AuthToken", res.token);

            Debug.Log($"Đăng nhập thành công! ID: {res.id}, Username: {res.username}");

            HideLoginPanel();

            if (uidText != null)
                uidText.text = $"UID: {res.id}";
            UpdateLoginUI();

        }
        else if (request.responseCode == 403)
        {
            feedbackText.text = "Tài khoản đã bị khóa. Vui lòng liên hệ hỗ trợ.";
            Debug.LogWarning("Tài khoản bị khóa: " + request.downloadHandler.text);
        }

        else
        {
            feedbackText.text = "Sai tài khoản hoặc lỗi kết nối.";
            Debug.Log("Response JSON: " + request.downloadHandler.text);
        }
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(false);
    }
    IEnumerator FetchSaveData()
    {
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(true);

        string token = PlayerPrefs.GetString("AuthToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Không có token để xác thực.");
            yield break;
        }

        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/GameData/get-save";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string saveJson = request.downloadHandler.text;
            Debug.Log("Dữ liệu save: " + saveJson);

            SaveData saveData = JsonUtility.FromJson<SaveData>(saveJson);
            if (!string.IsNullOrEmpty(saveData.currentSceneName))
            {
                SaveController.pendingSceneName = saveData.currentSceneName;
                UnityEngine.SceneManagement.SceneManager.LoadScene(saveData.currentSceneName);
            }
            else
            {
                Debug.LogWarning("Không có tên scene trong save data. Chuyển tới scene mặc định.");
            }
        }
        else
        {
            Logout();
            loginUI.SetActive(true);
            feedbackText.text = "Phiên đăng nhập hết hạn, vui lòng đăng nhập lại.";
            Debug.LogError("Lỗi khi lấy save: " + request.downloadHandler.text);
        }
        if (miniLoadingScreen != null) miniLoadingScreen.gameObject.SetActive(false);
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("AccountId");
        PlayerPrefs.Save();

        uidText.text = "UID: Chưa đăng nhập";

        UpdateLoginUI();
        HideLoginPanel();
        Debug.Log("Đã đăng xuất. Token và thông tin người dùng đã bị xoá.");
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

    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string id;
        public string username;
        public string token;
    }
}

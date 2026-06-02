using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class RelayUIAdapter : MonoBehaviour
{
    [Header("UI Page & Toggles")]
    [SerializeField] private GameObject relayPage;
    [SerializeField] private Button openPageButton;
    [SerializeField] private Button closePageButton;

    [Header("INTERNET - UI Host (Tạo Phòng)")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private TextMeshProUGUI joinCodeDisplayText;

    [Header("INTERNET - UI Client (Vào Phòng)")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinRoomButton;

    [Header("LAN - UI Host (Mạng Nội Bộ)")]
    [SerializeField] private Button createLANButton;
    [SerializeField] private TextMeshProUGUI lanInfoDisplayText;

    [Header("LAN - UI Client (Vào Mạng Nội Bộ)")]
    [SerializeField] private TMP_InputField lanIPInput;
    [SerializeField] private TMP_InputField lanPortInput;
    [SerializeField] private Button joinLANButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (openPageButton != null) openPageButton.onClick.AddListener(OpenPage);
        if (closePageButton != null) closePageButton.onClick.AddListener(ClosePage);

        if (relayPage != null) relayPage.SetActive(false);

        if (createRoomButton != null) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRoomButton != null) joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (createLANButton != null) createLANButton.onClick.AddListener(OnCreateLANClicked);
        if (joinLANButton != null) joinLANButton.onClick.AddListener(OnJoinLANClicked);

        if (joinCodeDisplayText != null) joinCodeDisplayText.text = "Chưa có mã phòng";
        if (lanInfoDisplayText != null) lanInfoDisplayText.text = "Chưa mở máy chủ LAN";
        if (statusText != null) statusText.text = "";
    }

    public void OpenPage()
    {
        if (!GameStateManager.CanProcessInput() || !GameStateManager.CanOpenMenu) return;

        if (relayPage != null) relayPage.SetActive(true);

        GameStateManager.IsMenuOpen = true;
        PauseController.SetPause(true);

        if (CommonUIController.Instance != null)
        {
            CommonUIController.Instance.SetUIVisible(false);
        }
    }

    public void ClosePage()
    {
        if (relayPage != null) relayPage.SetActive(false);

        GameStateManager.IsMenuOpen = false;

        if (!GameStateManager.IsDialogueActive)
        {
            PauseController.SetPause(false);
        }

        if (CommonUIController.Instance != null)
        {
            CommonUIController.Instance.SetUIVisible(true);
        }
    }

    private async void OnCreateRoomClicked()
    {
        SetUIInteractable(false);
        UpdateStatus("Đang chuyển đổi sang máy chủ Relay (Internet)...");

        string code = await RelayManager.Instance.CreateRelayHost();

        if (!string.IsNullOrEmpty(code))
        {
            joinCodeDisplayText.text = code;
            UpdateStatus("Tạo phòng Internet thành công! Đưa mã này cho bạn bè.");
            GUIUtility.systemCopyBuffer = code;
        }
        else
        {
            UpdateStatus("Lỗi: Không thể tạo phòng Internet.");
            SetUIInteractable(true);
        }
    }

    private async void OnJoinRoomClicked()
    {
        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";

        if (string.IsNullOrEmpty(code) || code.Length != 6)
        {
            UpdateStatus("Vui lòng nhập đúng mã phòng Internet (6 ký tự)!");
            return;
        }

        SetUIInteractable(false);
        UpdateStatus($"Đang kết nối tới phòng Internet {code}...");

        bool success = await RelayManager.Instance.JoinRelayClient(code);

        if (success)
        {
            UpdateStatus("Kết nối Internet thành công! Đang vào game...");
            ClosePage();
        }
        else
        {
            UpdateStatus("Kết nối thất bại. Mã phòng sai hoặc đã đầy.");
            SetUIInteractable(true);
        }
    }

    private void OnCreateLANClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            string localIP = RelayManager.GetLocalIPAddress();

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            ushort currentPort = transport.ConnectionData.Port;

            if (lanInfoDisplayText != null)
                lanInfoDisplayText.text = $"IP: {localIP}\nPort: {currentPort}";

            UpdateStatus("Mạng LAN đã mở sẵn! Đã copy thông tin vào bộ nhớ tạm.");
            GUIUtility.systemCopyBuffer = $"{localIP}:{currentPort}";
        }
        else
        {
            UpdateStatus("Lỗi: Máy chủ cục bộ chưa được khởi chạy.");
        }
    }

    private async void OnJoinLANClicked()
    {
        string ip = lanIPInput != null ? lanIPInput.text.Trim() : "";
        string portStr = lanPortInput != null ? lanPortInput.text.Trim() : "";

        if (string.IsNullOrEmpty(ip) || !ushort.TryParse(portStr, out ushort port))
        {
            UpdateStatus("Vui lòng nhập đúng định dạng IP và Port!");
            return;
        }

        SetUIInteractable(false);
        UpdateStatus($"Đang kết nối LAN tới {ip}:{port}...");

        bool success = await RelayManager.Instance.JoinLANClient(ip, port);

        if (success)
        {
            UpdateStatus("Kết nối LAN thành công! Đang vào game...");
            ClosePage();
        }
        else
        {
            UpdateStatus("Kết nối LAN thất bại. Vui lòng kiểm tra lại IP/Port.");
            SetUIInteractable(true);
        }
    }

    private void SetUIInteractable(bool state)
    {
        if (createRoomButton != null) createRoomButton.interactable = state;
        if (joinRoomButton != null) joinRoomButton.interactable = state;
        if (joinCodeInput != null) joinCodeInput.interactable = state;

        if (createLANButton != null) createLANButton.interactable = state;
        if (joinLANButton != null) joinLANButton.interactable = state;
        if (lanIPInput != null) lanIPInput.interactable = state;
        if (lanPortInput != null) lanPortInput.interactable = state;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
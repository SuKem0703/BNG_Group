using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class SystemStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI uidText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("FPS Settings")]
    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float fpsNextUpdateTime = 0f;

    private int currentPing = 0;
    private int currentFps = 0;
    private string pingColorHex = "green";

    private bool showPing = false;

    private void OnEnable()
    {
        SaveController.OnUIDReady += UpdateUIDText;
    }

    private void OnDisable()
    {
        SaveController.OnUIDReady -= UpdateUIDText;
    }

    private void Update()
    {
        CalculateAndDisplayStats();
    }

    private void UpdateUIDText(string uid)
    {
        if (uidText != null)
        {
            uidText.text = $"UID: {uid}";
        }
    }

    private void CalculateAndDisplayStats()
    {
        if (statsText == null) return;

        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrames++;

        if (Time.realtimeSinceStartup >= fpsNextUpdateTime)
        {
            float calculatedFps = fpsFrames / fpsAccumulator;
            currentFps = Mathf.RoundToInt(calculatedFps);

            float currentFpsInterval = calculatedFps >= 60f ? 1.0f : (calculatedFps >= 30f ? 2.0f : 5.0f);
            fpsNextUpdateTime = Time.realtimeSinceStartup + currentFpsInterval;

            fpsAccumulator = 0f;
            fpsFrames = 0;

            UpdateCoopPing();

            RefreshStatsDisplay();
        }
    }

    private void UpdateCoopPing()
    {
        showPing = false;
        currentPing = 0;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer)
        {
            showPing = true;
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport != null)
            {
                currentPing = (int)transport.GetCurrentRtt(NetworkManager.ServerClientId);
            }
        }

        if (currentPing < 100) pingColorHex = "green";
        else if (currentPing < 200) pingColorHex = "yellow";
        else pingColorHex = "red";
    }

    private void RefreshStatsDisplay()
    {
        if (statsText != null)
        {
            if (showPing)
            {
                statsText.text = $"<color={pingColorHex}>{currentPing} ms</color> - {currentFps} FPS";
            }
            else
            {
                statsText.text = $"{currentFps} FPS";
            }
        }
    }
}
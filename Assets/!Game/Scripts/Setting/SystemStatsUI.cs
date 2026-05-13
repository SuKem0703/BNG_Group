using UnityEngine;
using TMPro;

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

    private void OnEnable()
    {
        SaveController.OnUIDReady += UpdateUIDText;
        ServerTimeManager.OnPingUpdated += UpdatePingText;
    }

    private void OnDisable()
    {
        SaveController.OnUIDReady -= UpdateUIDText;
        ServerTimeManager.OnPingUpdated -= UpdatePingText;
    }

    private void Update()
    {
        CalculateAndDisplayFPS();
    }

    private void UpdateUIDText(string uid)
    {
        if (uidText != null)
        {
            uidText.text = $"UID: {uid}";
        }
    }

    private void UpdatePingText(int ping)
    {
        currentPing = ping;

        if (ping < 100) pingColorHex = "green";
        else if (ping < 200) pingColorHex = "yellow";
        else pingColorHex = "red";

        RefreshStatsDisplay();
    }

    private void CalculateAndDisplayFPS()
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

            RefreshStatsDisplay();
        }
    }

    private void RefreshStatsDisplay()
    {
        if (statsText != null)
        {
            statsText.text = $"<color={pingColorHex}>{currentPing} ms</color> - {currentFps} FPS";
        }
    }
}
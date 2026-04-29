using UnityEngine;
using TMPro;

public class SystemStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI uidText;
    [SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private TextMeshProUGUI fpsText;

    [Header("FPS Settings")]
    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float fpsNextUpdateTime = 0f;

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
        if (pingText == null) return;

        pingText.text = $"Ping: {ping} ms";

        if (ping < 100) pingText.color = Color.green;
        else if (ping < 200) pingText.color = Color.yellow;
        else pingText.color = Color.red;
    }

    private void CalculateAndDisplayFPS()
    {
        if (fpsText == null) return;

        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrames++;

        if (Time.realtimeSinceStartup >= fpsNextUpdateTime)
        {
            float currentFps = fpsFrames / fpsAccumulator;
            fpsText.text = $"FPS: {Mathf.RoundToInt(currentFps)}";

            float currentFpsInterval = currentFps >= 60f ? 1.0f : (currentFps >= 30f ? 2.0f : 5.0f);
            fpsNextUpdateTime = Time.realtimeSinceStartup + currentFpsInterval;

            fpsAccumulator = 0f;
            fpsFrames = 0;
        }
    }
}
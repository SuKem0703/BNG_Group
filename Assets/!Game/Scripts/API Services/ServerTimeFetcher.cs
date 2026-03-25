using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTimeManager : MonoBehaviour
{
    public static DateTime ServerTime { get; private set; }
    public static float LocalTimeAtFetch { get; private set; }

    public static int CurrentPing { get; private set; } = 0;

    public TMPro.TextMeshProUGUI pingText;
    public TMPro.TextMeshProUGUI fpsText;

    private const float REFRESH_INTERVAL = 300f;
    private const double TIME_CHEAT_THRESHOLD = 120.0f;

    private bool autoSaveStarted = false;

    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float fpsNextUpdateTime = 0f;
    private float currentFpsInterval = 5.0f;

    private void Start()
    {
        if (pingText == null)
        {
            pingText = GameObject.Find("PingText")?.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (fpsText == null)
        {
            fpsText = GameObject.Find("FPSText")?.GetComponent<TMPro.TextMeshProUGUI>();
        }
    }

    void Update()
    {
        int ping = ServerTimeManager.CurrentPing;
        if (pingText != null)
        {
            pingText.text = $"Ping: {ping} ms";

            if (ping < 100)
            {
                pingText.color = Color.green;
            }
            else if (ping < 200)
            {
                pingText.color = Color.yellow;
            }
            else
            {
                pingText.color = Color.red;
            }
        }

        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrames++;

        if (Time.realtimeSinceStartup >= fpsNextUpdateTime)
        {
            float currentFps = fpsFrames / fpsAccumulator;

            if (fpsText != null)
            {
                fpsText.text = $"FPS: {Mathf.RoundToInt(currentFps)}";
            }

            // Cập nhật lại logic thời gian làm mới FPS
            if (currentFps >= 60f)
                currentFpsInterval = 1.0f;
            else if (currentFps >= 30f)
                currentFpsInterval = 2.0f;
            else
                currentFpsInterval = 5.0f;

            fpsNextUpdateTime = Time.realtimeSinceStartup + currentFpsInterval;
            fpsAccumulator = 0f;
            fpsFrames = 0;
        }
    }

    void OnEnable()
    {
        SaveController.OnDataLoaded += StartAutoSaveRoutine;
    }

    void OnDisable()
    {
        SaveController.OnDataLoaded -= StartAutoSaveRoutine;
    }

    private void StartAutoSaveRoutine()
    {
        if (autoSaveStarted) return;
        autoSaveStarted = true;
        StartCoroutine(AutoTaskRoutine());
    }

    private IEnumerator AutoTaskRoutine()
    {
        yield return FetchServerTime();

        // Tách riêng luồng gọi Ping mỗi 5 giây
        StartCoroutine(PingRoutine());

        while (true)
        {
            yield return new WaitForSeconds(REFRESH_INTERVAL);
            SaveController.Instance?.TriggerAutoSave();
        }
    }

    // Coroutine cập nhật Ping liên tục mỗi 5 giây
    private IEnumerator PingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5.0f);
            yield return FetchServerTime();
        }
    }

    public IEnumerator FetchServerTime()
    {
        string url = NetworkConfig.GetUrl("api/GameData/ping");

        float requestStartTime = Time.realtimeSinceStartup;

        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        float requestEndTime = Time.realtimeSinceStartup;

        float rttMs = (requestEndTime - requestStartTime) * 1000f;
        CurrentPing = Mathf.RoundToInt(rttMs);

        if (request.result == UnityWebRequest.Result.Success)
        {
            PingResponse response = JsonUtility.FromJson<PingResponse>(request.downloadHandler.text);

            if (DateTime.TryParse(response.serverTime, out DateTime fetchedTime))
            {
                DateTime preciseServerTime = fetchedTime.AddMilliseconds(rttMs / 2.0f);

                ServerTime = preciseServerTime;
                LocalTimeAtFetch = Time.time;

                CheckTimeTampering(preciseServerTime);
            }
        }
        else
        {
            Debug.LogWarning($"[Ping Error] Không thể kết nối server: {request.error}");
            if (ServerTime == default)
            {
                ServerTime = DateTime.Now;
                LocalTimeAtFetch = Time.time;
            }
        }
    }

    private void CheckTimeTampering(DateTime trustedServerTime)
    {
        DateTime systemTime = DateTime.Now;

        double driftSeconds = Math.Abs((systemTime - trustedServerTime).TotalSeconds);

        double allowedDrift = TIME_CHEAT_THRESHOLD + (CurrentPing / 1000.0f);

        if (driftSeconds > allowedDrift)
        {
            string warningMsg = $"[ANTI-CHEAT WARNING] Giờ máy tính lệch quá nhiều so với Server! " +
                                $"System: {systemTime}, Server: {trustedServerTime}, " +
                                $"Diff: {driftSeconds:F2}s, Ping: {CurrentPing}ms";

            Debug.LogWarning(warningMsg);
        }
    }

    public void HandleAppFocusLoss(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveController.Instance?.TriggerAutoSave();
        }
    }

    public static DateTime GetCurrentTime()
    {
        if (LocalTimeAtFetch == 0) return DateTime.Now;
        return ServerTime.AddSeconds(Time.time - LocalTimeAtFetch);
    }

    public static void ReportPing(float requestDurationSeconds)
    {
        int newPing = Mathf.RoundToInt(requestDurationSeconds * 1000f);

        if (CurrentPing == 0) CurrentPing = newPing;
        else CurrentPing = Mathf.RoundToInt(Mathf.Lerp(CurrentPing, newPing, 0.5f));
    }
}

[System.Serializable]
public class PingResponse
{
    public string message;
    public string serverTime;
}
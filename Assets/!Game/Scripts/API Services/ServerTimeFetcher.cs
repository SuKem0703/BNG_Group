using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTimeManager : MonoBehaviour
{
    public static DateTime ServerTime { get; private set; }
    public static float LocalTimeAtFetch { get; private set; }
    public static int CurrentPing { get; private set; } = 0;

    public static event System.Action<int> OnPingUpdated;

    private const float REFRESH_INTERVAL = 300f;
    private const double TIME_CHEAT_THRESHOLD = 120.0f;

    private bool autoSaveStarted = false;

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

        StartCoroutine(PingRoutine());

        while (true)
        {
            yield return new WaitForSeconds(REFRESH_INTERVAL);
            SaveController.Instance?.TriggerAutoSave();
        }
    }

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

        OnPingUpdated?.Invoke(CurrentPing);

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

        OnPingUpdated?.Invoke(CurrentPing);
    }
}

[System.Serializable]
public class PingResponse
{
    public string message;
    public string serverTime;
}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTimeManager : MonoBehaviour
{
    public static DateTime ServerTime { get; private set; }
    public static float LocalTimeAtFetch { get; private set; }

    private const float REFRESH_INTERVAL = 300f;
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
        while (true)
        {
            yield return FetchServerTime();

            SaveController.Instance?.TriggerAutoSave();

            yield return new WaitForSeconds(REFRESH_INTERVAL);
        }
    }

    public IEnumerator FetchServerTime()
    {
        string url = NetworkConfig.GetUrl("api/GameData/ping");

        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            PingResponse response = JsonUtility.FromJson<PingResponse>(request.downloadHandler.text);

            if (DateTime.TryParse(response.serverTime, out DateTime fetchedTime))
            {
                ServerTime = Math.Abs((fetchedTime - DateTime.Now).TotalSeconds) <= 10 ? DateTime.Now : fetchedTime;
                LocalTimeAtFetch = Time.time;

                // Debug.Log($"[TimeSync] Server: {fetchedTime} | Local: {DateTime.Now}");
            }
        }
        else
        {
            Debug.LogWarning($"Lỗi lấy giờ server: {request.error}");

            if (ServerTime == default)
            {
                ServerTime = DateTime.Now;
                LocalTimeAtFetch = Time.time;
            }
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
}

[System.Serializable]
public class PingResponse
{
    public string message;
    public string serverTime;
}
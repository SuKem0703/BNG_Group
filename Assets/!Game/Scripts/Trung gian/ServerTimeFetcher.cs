using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Quản lý lấy giờ server, đồng bộ và auto-save.
/// </summary>
public class ServerTimeManager : MonoBehaviour
{
    public static DateTime ServerTime { get; private set; }
    public static float LocalTimeAtFetch { get; private set; }

    private SaveController saveController;
    private const float REFRESH_INTERVAL = 300f;
    private bool autoSaveStarted = false;

    void Awake()
    {
        saveController = FindFirstObjectByType<SaveController>();
        if (saveController == null)
        {
            Debug.LogWarning("ServerTimeManager: Không tìm thấy SaveController. Auto-save sẽ không hoạt động.");
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
        while (true)
        {
            yield return FetchServerTime();

            saveController?.SaveGame();

            yield return new WaitForSeconds(REFRESH_INTERVAL);
        }
    }

    public IEnumerator FetchServerTime()
    {
        const string url = "https://timeapi.io/api/Time/current/zone?timeZone=Asia/Ho_Chi_Minh";
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            TimeApiResponse response = JsonUtility.FromJson<TimeApiResponse>(request.downloadHandler.text);
            DateTime fetchedTime = DateTime.Parse(response.dateTime);

            ServerTime = Math.Abs((fetchedTime - DateTime.Now).TotalSeconds) <= 10 ? DateTime.Now : fetchedTime;
            LocalTimeAtFetch = Time.time;
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

    /// <summary>
    /// Lưu game khi tab ra ngoài.
    /// </summary>
    public void HandleAppFocusLoss(bool hasFocus)
    {
        if (!hasFocus)
        {
            saveController?.SaveGame();
        }
    }
}

[System.Serializable]
public class TimeApiResponse
{
    public string dateTime;
    // Các field khác giữ nguyên nếu cần, nhưng thực tế bạn chỉ dùng dateTime
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTimeFetcher : MonoBehaviour
{
    public static DateTime ServerTime { get; private set; }
    public static float LocalTimeAtFetch { get; private set; }

    private SaveController saveController;
    private const float REFRESH_INTERVAL = 300f;
    private bool autoSaveStarted = false;
    void Start()
    {
        saveController = FindFirstObjectByType<SaveController>();
        if (saveController == null)
        {
            Debug.LogWarning("ServerTimeFetcher: Không tìm thấy SaveController. Tính năng AutoSave sẽ không hoạt động.");
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
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (saveController != null && SaveController.IsDataLoaded)
            {
                //Debug.Log("[ApplicationFocus] Người chơi đã tab ra ngoài. Buộc lưu game...");
                saveController.SaveGame();
            }
        }
    }
    private IEnumerator AutoTaskRoutine()
    {
        while (true)
        {
            Debug.Log("[AutoTask] Đang lấy giờ server và đồng bộ...");
            yield return StartCoroutine(FetchServerTime());

            if (saveController != null)
            {
                Debug.Log("[AutoTask] Đang tự động lưu game...");
                saveController.SaveGame();
            }

            Debug.Log($"[AutoTask] Đã xong. Chờ {REFRESH_INTERVAL} giây.");
            yield return new WaitForSeconds(REFRESH_INTERVAL);
        }
    }

    public IEnumerator FetchServerTime()
    {
        string url = "https://timeapi.io/api/Time/current/zone?timeZone=Asia/Ho_Chi_Minh";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            TimeApiResponse timeData = JsonUtility.FromJson<TimeApiResponse>(json);

            DateTime fetchedServerTime = DateTime.Parse(timeData.dateTime);

            DateTime localSystemTime = DateTime.Now;

            double diffInSeconds = Math.Abs((fetchedServerTime - localSystemTime).TotalSeconds);

            if (diffInSeconds <= 10)
            {
                ServerTime = localSystemTime;
            }
            else
            {
                ServerTime = fetchedServerTime;
                Debug.Log("[Time Sync] Thời gian đáng ngờ, đồng bộ về giờ server.");
            }

            LocalTimeAtFetch = Time.time;
        }
        else
        {
            Debug.LogWarning("Lỗi lấy giờ từ server: " + request.error);
            if (ServerTime == default)
            {
                ServerTime = DateTime.Now;
                LocalTimeAtFetch = Time.time;
            }
        }
    }
}

[System.Serializable]
public class TimeApiResponse
{
    public int year;
    public int month;
    public int day;
    public int hour;
    public int minute;
    public int seconds;
    public int milliSeconds;
    public string dateTime;
    public string date;
    public string time;
    public string timeZone;
    public string dayOfWeek;
    public bool dstActive;
}
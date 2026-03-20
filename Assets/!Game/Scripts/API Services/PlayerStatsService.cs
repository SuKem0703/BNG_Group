using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerStatsService : MonoBehaviour
{
    public static PlayerStatsService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    [System.Serializable]
    public class ServerUserStat
    {
        public int level;
        public int exp;
        public int potentialPoints;
        public int str;
        public int dex;
        public int intStat;
        public int con;
    }

    [System.Serializable]
    public class DistributeRequest { public string statType; public int amount; }

    public void SyncProfile(System.Action<bool> onComplete = null)
    {
        StartCoroutine(SyncRoutine(onComplete));
    }

    private IEnumerator SyncRoutine(System.Action<bool> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/PlayerStats/profile");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text.Replace("\"int\":", "\"intStat\":");

            var data = JsonUtility.FromJson<ServerUserStat>(json);

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.SyncStatsFromServer(data);
            }
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError("Lỗi Sync Stats: " + request.error);
            onComplete?.Invoke(false);
        }
    }

    public void DistributePoint(string statType, int amount, System.Action<bool> onComplete)
    {
        StartCoroutine(DistributeRoutine(statType, amount, onComplete));
    }

    private IEnumerator DistributeRoutine(string statType, int amount, System.Action<bool> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/PlayerStats/distribute");
        string token = PlayerPrefs.GetString("AuthToken", "");

        string json = JsonUtility.ToJson(new DistributeRequest { statType = statType, amount = amount });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text.Replace("\"int\":", "\"intStat\":");
            var newData = JsonUtility.FromJson<ServerUserStat>(responseJson);

            if (PlayerStats.Instance != null)
                PlayerStats.Instance.SyncStatsFromServer(newData);

            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogWarning("Lỗi cộng điểm: " + request.downloadHandler.text);
            onComplete?.Invoke(false);
        }
    }

    public void ResetStats(System.Action<bool> onComplete)
    {
        StartCoroutine(ResetStatsRoutine(onComplete));
    }

    private IEnumerator ResetStatsRoutine(System.Action<bool> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/PlayerStats/reset");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {token}");
        request.SetRequestHeader("Content-Length", "0");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<ResetResponse>(request.downloadHandler.text);

            if (response.success)
            {
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.SyncStatsFromServer(response.newStats);
                    PlayerStats.Instance.SyncCurrency(response.coin, response.gem);
                }
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("Reset thất bại: " + response.message);
                onComplete?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError("Network Error: " + request.error);
            onComplete?.Invoke(false);
        }
    }

    [System.Serializable]
    public class ResetResponse
    {
        public bool success;
        public string message;
        public ServerUserStat newStats;
        public int coin;
        public int gem;
    }

    [System.Serializable]
    public class AddExpResponse
    {
        public bool success;
        public bool leveledUp;
        public ServerUserStat newStats;
    }

    [System.Serializable]
    public class AddExpBody
    {
        public int amount;
    }

    public void AddExp(int amount)
    {
        StartCoroutine(AddExpRoutine(amount));
    }

    private IEnumerator AddExpRoutine(int amount)
    {
        string url = NetworkConfig.GetUrl("api/PlayerStats/add-exp");
        string token = PlayerPrefs.GetString("AuthToken", "");

        AddExpBody body = new AddExpBody { amount = amount };
        string json = JsonUtility.ToJson(body);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        Debug.Log($"[Server Response] Code: {request.responseCode} | Body: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            string resJson = request.downloadHandler.text.Replace("\"int\":", "\"intStat\":");
            var res = JsonUtility.FromJson<AddExpResponse>(resJson);

            if (res.success && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.SyncStatsFromServer(res.newStats);

                if (res.leveledUp)
                {
                    PlayerStats.Instance.PlayLevelUpEffect();
                }
            }
        }
        else
        {
            Debug.LogError("Lỗi cộng EXP: " + request.downloadHandler.text);
        }
    }
}
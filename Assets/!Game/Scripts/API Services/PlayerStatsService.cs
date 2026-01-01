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

    // Class khớp với JSON từ Server UserStat
    [System.Serializable]
    public class ServerUserStat
    {
        public int level;
        public int exp;
        public int potentialPoints;
        public int str;
        public int dex;
        public int intStat; // Lưu ý: Server trả về "int" hoặc "Int", cần map đúng tên biến JSON nếu dùng JsonUtility
        // Để an toàn, ở server trả về PascalCase, nhưng JsonUtility Unity hơi kén. 
        // Tốt nhất dùng thư viện Newtonsoft.Json hoặc sửa DTO server trả về chữ thường.
        // Ở đây giả định bạn đã handle mapping (xem lưu ý dưới).
        public int con;
    }

    // DTO Request
    [System.Serializable]
    public class DistributeRequest { public string StatType; public int Amount; }

    // 1. Lấy thông tin (Sync)
    public void SyncProfile(System.Action<bool> onComplete = null)
    {
        StartCoroutine(SyncRoutine(onComplete));
    }

    private IEnumerator SyncRoutine(System.Action<bool> onComplete)
    {
        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/PlayerStats/profile";
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // MẸO: Thay thế "int": thành "intStat": để JsonUtility hiểu được
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

    // 2. Cộng điểm
    public void DistributePoint(string statType, int amount, System.Action<bool> onComplete)
    {
        StartCoroutine(DistributeRoutine(statType, amount, onComplete));
    }

    private IEnumerator DistributeRoutine(string statType, int amount, System.Action<bool> onComplete)
    {
        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/PlayerStats/distribute";
        string token = PlayerPrefs.GetString("AuthToken", "");

        // Gửi amount động thay vì hardcode số 1
        string json = JsonUtility.ToJson(new DistributeRequest { StatType = statType, Amount = amount });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Update lại stats chuẩn từ server trả về
            // Mẹo: Replace để fix vụ intStat nếu cần
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

    // API Reset Stats
    public void ResetStats(System.Action<bool> onComplete)
    {
        StartCoroutine(ResetStatsRoutine(onComplete));
    }

    private IEnumerator ResetStatsRoutine(System.Action<bool> onComplete)
    {
        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/PlayerStats/reset";
        string token = PlayerPrefs.GetString("AuthToken", "");

        // Gửi POST rỗng (Server tự biết trừ 20 Gem)
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {token}");
        request.SetRequestHeader("Content-Length", "0"); // Quan trọng với POST rỗng

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Server trả về cấu trúc gồm cả Stats mới và Balance mới
            var response = JsonUtility.FromJson<ResetResponse>(request.downloadHandler.text);

            if (response.success)
            {
                // Cập nhật Stats
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

    // Class nhận phản hồi Reset
    [System.Serializable]
    public class ResetResponse
    {
        public bool success;
        public string message;
        public ServerUserStat newStats; // Stats mới sau khi reset
        public int coin;
        public int gem; // Gem mới sau khi trừ
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
        public int Amount;
    }
    public void AddExp(int amount)
    {
        StartCoroutine(AddExpRoutine(amount));
    }

    private IEnumerator AddExpRoutine(int amount)
    {
        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/PlayerStats/add-exp";
        string token = PlayerPrefs.GetString("AuthToken", "");

        AddExpBody body = new AddExpBody { Amount = amount };
        string json = JsonUtility.ToJson(body);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

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
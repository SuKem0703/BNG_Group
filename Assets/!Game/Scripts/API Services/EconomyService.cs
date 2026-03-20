using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EconomyService : MonoBehaviour
{
    public static EconomyService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    [System.Serializable]
    public class SpendRequest
    {
        public string currencyType;
        public int amount;
        public string reason;
    }

    [System.Serializable]
    public class EconomyResponse
    {
        public bool success;
        public int newBalance;
        public string message;
    }

    public void SpendCurrency(string type, int amount, string reason, System.Action<bool> onComplete)
    {
        StartCoroutine(SpendRoutine(type, amount, reason, onComplete));
    }

    private IEnumerator SpendRoutine(string type, int amount, string reason, System.Action<bool> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Economy/spend");
        string token = PlayerPrefs.GetString("AuthToken", "");

        SpendRequest reqBody = new SpendRequest { currencyType = type, amount = amount, reason = reason };
        string json = JsonUtility.ToJson(reqBody);

        Debug.Log($"[Economy] Requesting Spend: {json}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;

        yield return request.SendWebRequest();

        ServerTimeManager.ReportPing(Time.realtimeSinceStartup - startTime);

        Debug.Log($"[Economy] Server Status: {request.responseCode}");
        Debug.Log($"[Economy] Raw Response: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<EconomyResponse>(request.downloadHandler.text);

            if (response.success)
            {
                if (type == "Coin")
                    PlayerStats.Instance.SyncCoinFromServer(response.newBalance);
                else
                    PlayerStats.Instance.SyncGemFromServer(response.newBalance);

                Debug.Log($"[Economy] Success! New Balance: {response.newBalance}");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogWarning($"[Economy] Giao dịch thất bại: {response.message}");
                Debug.LogWarning($"[Economy] Server Balance: {response.newBalance} | Client Balance: {(type == "Coin" ? PlayerStats.Instance.coin : PlayerStats.Instance.gem)}");

                if (type == "Coin")
                    PlayerStats.Instance.SyncCoinFromServer(response.newBalance);
                else
                    PlayerStats.Instance.SyncGemFromServer(response.newBalance);

                onComplete?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError($"[Economy] Network Error: {request.error} | Response: {request.downloadHandler.text}");
            onComplete?.Invoke(false);
        }
    }

    [System.Serializable]
    public class BalanceResponse
    {
        public int coin;
        public int gem;
    }

    public void RefreshBalance()
    {
        StartCoroutine(GetBalanceRoutine(null));
    }

    private IEnumerator GetBalanceRoutine(System.Action<int, int> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Economy/balance");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        if (request.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<BalanceResponse>(request.downloadHandler.text);

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.SyncCurrency(res.coin, res.gem);
            }
        }
        else
        {
            Debug.LogError("Lỗi lấy số dư: " + request.error);
        }
    }

    public void EarnCurrency(string type, int amount, string reason, System.Action<bool> onComplete)
    {
        StartCoroutine(EarnRoutine(type, amount, reason, onComplete));
    }

    private IEnumerator EarnRoutine(string type, int amount, string reason, System.Action<bool> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Economy/earn");
        string token = PlayerPrefs.GetString("AuthToken", "");

        SpendRequest reqBody = new SpendRequest { currencyType = type, amount = amount, reason = reason };
        string json = JsonUtility.ToJson(reqBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;

        yield return request.SendWebRequest();

        ServerTimeManager.ReportPing(Time.realtimeSinceStartup - startTime);

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<EconomyResponse>(request.downloadHandler.text);
            if (response.success)
            {
                if (type == "Coin") PlayerStats.Instance.SyncCoinFromServer(response.newBalance);
                else PlayerStats.Instance.SyncGemFromServer(response.newBalance);

                onComplete?.Invoke(true);
            }
            else
            {
                onComplete?.Invoke(false);
            }
        }
        else
        {
            onComplete?.Invoke(false);
        }
    }
}
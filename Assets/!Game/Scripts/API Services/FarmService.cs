using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FarmService : MonoBehaviour
{
    public static FarmService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    [Serializable]
    public class ServerFarmPlot
    {
        public string plotId;
        public int seedItemId;
        public string plantedAt;
    }

    [Serializable]
    public class SyncFarmResponse
    {
        public string serverTime;
        public List<ServerFarmPlot> plots;
    }

    [Serializable] public class PlantRequest { public string plotId; public int seedItemId; }

    // DTO Hỗ trợ gửi hàng loạt
    [Serializable] public class HarvestAction { public string plotId; public bool isRegrowable; public float offsetSeconds; }
    [Serializable] public class BulkHarvestRequest { public List<HarvestAction> actions; }

    // --- BIẾN PHỤC VỤ HÀNG ĐỢI (DEBOUNCE) ---
    private List<HarvestAction> pendingHarvests = new List<HarvestAction>();
    private Coroutine debounceCoroutine;
    private float debounceWaitTime = 1.5f; // Chờ 1.5s sau thao tác cuối cùng mới gửi đi

    public void SyncFarm(Action<List<ServerFarmPlot>> onComplete)
    {
        StartCoroutine(SyncRoutine(onComplete));
    }

    private IEnumerator SyncRoutine(Action<List<ServerFarmPlot>> onComplete)
    {
        string url = NetworkConfig.GetUrl("api/Farm/sync");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var res = JsonUtility.FromJson<SyncFarmResponse>(request.downloadHandler.text);
            onComplete?.Invoke(res.plots);
        }
        else
        {
            Debug.LogError("Farm Sync Failed: " + request.error);
            onComplete?.Invoke(new List<ServerFarmPlot>());
        }
    }

    public void RequestPlant(string plotId, int seedItemId)
    {
        StartCoroutine(PostRequest("api/Farm/plant", new PlantRequest { plotId = plotId, seedItemId = seedItemId }));
    }

    // --- HÀM THU HOẠCH ĐÃ ĐƯỢC TỐI ƯU GOM LÔ ---
    public void RequestHarvest(string plotId, bool isRegrowable, float offsetSeconds)
    {
        pendingHarvests.Add(new HarvestAction
        {
            plotId = plotId,
            isRegrowable = isRegrowable,
            offsetSeconds = offsetSeconds
        });

        if (debounceCoroutine != null) StopCoroutine(debounceCoroutine);
        debounceCoroutine = StartCoroutine(ProcessHarvestBatch());
    }

    private IEnumerator ProcessHarvestBatch()
    {
        yield return new WaitForSeconds(debounceWaitTime);

        if (pendingHarvests.Count == 0) yield break;

        List<HarvestAction> batchToSend = new List<HarvestAction>(pendingHarvests);
        pendingHarvests.Clear();

        BulkHarvestRequest body = new BulkHarvestRequest { actions = batchToSend };

        Debug.Log($"[Farm] Bắt đầu gửi Bulk Harvest gồm {batchToSend.Count} ô đất...");

        yield return StartCoroutine(PostRequest("api/Farm/harvest", body));
    }

    private IEnumerator PostRequest(string endpoint, object body)
    {
        string url = NetworkConfig.GetUrl(endpoint);
        string token = PlayerPrefs.GetString("AuthToken", "");
        string json = JsonUtility.ToJson(body);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[FarmService] API {endpoint} Failed: {request.downloadHandler.text}");
        }
    }
}
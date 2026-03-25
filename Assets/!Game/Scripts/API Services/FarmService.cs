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
    [Serializable] public class HarvestRequest { public string plotId; public bool isRegrowable; public float offsetSeconds; }

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

    public void RequestHarvest(string plotId, bool isRegrowable, float offsetSeconds)
    {
        StartCoroutine(PostRequest("api/Farm/harvest", new HarvestRequest { plotId = plotId, isRegrowable = isRegrowable, offsetSeconds = offsetSeconds }));
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
    }
}
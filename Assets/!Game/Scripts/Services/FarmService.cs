using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FarmService : MonoBehaviour
{
    public static FarmService Instance { get; private set; }

    private ProfileRepository profileRepo;
    private FarmData localFarmData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        profileRepo = new ProfileRepository();
    }

    private string CurrentProfileId => PlayerPrefs.GetString("CurrentProfileId", "");

    private void EnsureLocalDataLoaded()
    {
        if (localFarmData != null) return;

        ProfileEntity profile = profileRepo.GetProfile(CurrentProfileId);
        if (profile != null && !string.IsNullOrEmpty(profile.FarmSaveDataJson))
        {
            localFarmData = JsonUtility.FromJson<FarmData>(profile.FarmSaveDataJson);
        }

        if (localFarmData == null) localFarmData = new FarmData();
    }

    private void SaveLocalData()
    {
        ProfileEntity profile = profileRepo.GetProfile(CurrentProfileId);
        if (profile != null)
        {
            profile.FarmSaveDataJson = JsonUtility.ToJson(localFarmData);
            profileRepo.UpdateProfile(profile);
        }
    }

    public void SyncFarm(Action<List<ServerFarmPlot>> onComplete)
    {
        EnsureLocalDataLoaded();

        List<ServerFarmPlot> plots = new List<ServerFarmPlot>();
        foreach (var plotData in localFarmData.plotDataList)
        {
            if (plotData.hasCrop && plotData.cropData != null)
            {
                string timeString = new DateTime(plotData.cropData.lastSaveTime, DateTimeKind.Utc).ToString("o");
                plots.Add(new ServerFarmPlot
                {
                    plotId = plotData.plotID,
                    seedItemId = plotData.cropData.seedItemID,
                    plantedAt = timeString
                });
            }
        }

        onComplete?.Invoke(plots);
    }

    public void RequestPlant(string plotId, int seedItemId)
    {
        EnsureLocalDataLoaded();

        FarmPlotSaveData existingPlot = localFarmData.plotDataList.FirstOrDefault(p => p.plotID == plotId);

        if (existingPlot == null)
        {
            existingPlot = new FarmPlotSaveData { plotID = plotId };
            localFarmData.plotDataList.Add(existingPlot);
        }

        existingPlot.hasCrop = true;
        existingPlot.cropData = new CropSaveData
        {
            seedItemID = seedItemId,
            currentStage = 0,
            currentTimer = 0,
            lastSaveTime = ServerTimeManager.GetCurrentTime().ToUniversalTime().Ticks
        };

        SaveLocalData();
        Debug.Log($"[Farm-Offline] Đã gieo hạt {seedItemId} tại ô {plotId}");
    }

    public void RequestDestroy(string plotId)
    {
        EnsureLocalDataLoaded();

        FarmPlotSaveData existingPlot = localFarmData.plotDataList.FirstOrDefault(p => p.plotID == plotId);
        if (existingPlot != null)
        {
            existingPlot.hasCrop = false;
            existingPlot.cropData = null;
            SaveLocalData();
            Debug.Log($"[Farm-Offline] Đã hủy cây tại ô {plotId}");
        }
    }

    private List<string> pendingHarvests = new List<string>();
    private float harvestDebounceTimer = 0f;
    private bool isHarvestTimerRunning = false;

    private void Update()
    {
        if (isHarvestTimerRunning)
        {
            harvestDebounceTimer -= Time.deltaTime;
            if (harvestDebounceTimer <= 0)
            {
                ProcessPendingHarvests();
            }
        }
    }

    public void RequestHarvest(string plotId)
    {
        EnsureLocalDataLoaded();

        FarmPlotSaveData existingPlot = localFarmData.plotDataList.FirstOrDefault(p => p.plotID == plotId);
        if (existingPlot != null && existingPlot.hasCrop)
        {
            existingPlot.hasCrop = false;
            existingPlot.cropData = null;

            SaveLocalData();
        }
    }

    public void ForceSendPendingHarvests()
    {
        if (pendingHarvests.Count > 0)
        {
            ProcessPendingHarvests();
        }
    }

    private void ProcessPendingHarvests()
    {
        isHarvestTimerRunning = false;
        if (pendingHarvests.Count == 0) return;

        EnsureLocalDataLoaded();

        List<string> batch = new List<string>(pendingHarvests);
        pendingHarvests.Clear();

        int processedCount = 0;
        foreach (string plotId in batch)
        {
            FarmPlotSaveData existingPlot = localFarmData.plotDataList.FirstOrDefault(p => p.plotID == plotId);
            if (existingPlot != null && existingPlot.hasCrop)
            {
                existingPlot.hasCrop = false;
                existingPlot.cropData = null;
                processedCount++;
            }
        }

        if (processedCount > 0)
        {
            SaveLocalData();
            Debug.Log($"[Farm-Offline] Đã chốt sổ thu hoạch {processedCount} ô đất!");
        }
    }

    [Serializable]
    public class ServerFarmPlot
    {
        public string plotId;
        public int seedItemId;
        public string plantedAt;
    }
}
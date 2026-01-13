using System;
using System.Collections.Generic;

[Serializable]
public class FarmData
{
    public List<FarmPlotSaveData> plotDataList = new List<FarmPlotSaveData>();
}

[Serializable]
public class FarmPlotSaveData
{
    public string plotID;
    public bool hasCrop;
    public CropSaveData cropData;
}

[Serializable]
public class CropSaveData
{
    public int seedItemID;
    public int currentStage;
    public float currentTimer;

    // (Optional) Lưu timestamp để tính offline growth sau này
    // public long lastSaveTime; 
}
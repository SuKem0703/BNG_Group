using System;
using System.Collections;
using UnityEngine;

public class ServerTimeManager : MonoBehaviour
{
    public static event System.Action<int> OnPingUpdated;

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
            yield return new WaitForSecondsRealtime(REFRESH_INTERVAL);
            SaveController.Instance?.TriggerAutoSave();
        }
    }

    public static DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }

    public static void ReportPing(float requestDurationSeconds)
    {
        OnPingUpdated?.Invoke(0);
    }

    public void HandleAppFocusLoss(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveController.Instance?.TriggerAutoSave();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotentialService : MonoBehaviour
{
    public static PotentialService Instance { get; private set; }

    private Dictionary<string, int> pendingPoints = new Dictionary<string, int>();
    private Dictionary<string, int> sendingPoints = new Dictionary<string, int>();

    private Coroutine batchSendCoroutine;
    private float debounceTime = 1.5f;

    public bool IsDirty { get; private set; }

    private bool isSending = false;

    public event Action OnPendingPointsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pendingPoints["STR"] = 0; pendingPoints["DEX"] = 0; pendingPoints["CON"] = 0; pendingPoints["INT"] = 0;
        sendingPoints["STR"] = 0; sendingPoints["DEX"] = 0; sendingPoints["CON"] = 0; sendingPoints["INT"] = 0;

        ResetPendingState();
    }

    public void ResetPendingState()
    {
        pendingPoints["STR"] = 0; pendingPoints["DEX"] = 0; pendingPoints["CON"] = 0; pendingPoints["INT"] = 0;
        sendingPoints["STR"] = 0; sendingPoints["DEX"] = 0; sendingPoints["CON"] = 0; sendingPoints["INT"] = 0;
        IsDirty = false;
        isSending = false;
        OnPendingPointsChanged?.Invoke();
    }

    public int GetPending(string stat) => pendingPoints.ContainsKey(stat) ? pendingPoints[stat] : 0;
    public int GetSending(string stat) => sendingPoints.ContainsKey(stat) ? sendingPoints[stat] : 0;

    public int GetTotalPending()
    {
        int p = GetPending("STR") + GetPending("DEX") + GetPending("CON") + GetPending("INT");
        int s = GetSending("STR") + GetSending("DEX") + GetSending("CON") + GetSending("INT");
        return p + s;
    }

    public void IncreaseStat(string statName, int currentTotalPotential)
    {
        if (currentTotalPotential - GetTotalPending() <= 0) return;

        pendingPoints[statName]++;
        IsDirty = true;

        OnPendingPointsChanged?.Invoke();

        if (batchSendCoroutine != null) StopCoroutine(batchSendCoroutine);

        if (!isSending)
        {
            batchSendCoroutine = StartCoroutine(DebounceSend());
        }
    }

    private IEnumerator DebounceSend()
    {
        yield return new WaitForSeconds(debounceTime);
        SendBatchRequestNow();
    }

    public void SendBatchRequestNow()
    {
        if (!IsDirty || isSending) return;

        int addStr = pendingPoints["STR"];
        int addDex = pendingPoints["DEX"];
        int addCon = pendingPoints["CON"];
        int addInt = pendingPoints["INT"];

        if (addStr == 0 && addDex == 0 && addCon == 0 && addInt == 0)
        {
            IsDirty = false;
            return;
        }

        sendingPoints["STR"] = addStr;
        sendingPoints["DEX"] = addDex;
        sendingPoints["CON"] = addCon;
        sendingPoints["INT"] = addInt;

        pendingPoints["STR"] = 0;
        pendingPoints["DEX"] = 0;
        pendingPoints["CON"] = 0;
        pendingPoints["INT"] = 0;

        IsDirty = false;
        isSending = true;

        PlayerStatsService.Instance.DistributePoints(addStr, addDex, addInt, addCon, (success) =>
        {
            isSending = false;

            if (success)
            {
                sendingPoints["STR"] = 0;
                sendingPoints["DEX"] = 0;
                sendingPoints["CON"] = 0;
                sendingPoints["INT"] = 0;

                OnPendingPointsChanged?.Invoke();

                if (GetPending("STR") > 0 || GetPending("DEX") > 0 || GetPending("CON") > 0 || GetPending("INT") > 0)
                {
                    IsDirty = true;
                    if (batchSendCoroutine != null) StopCoroutine(batchSendCoroutine);
                    batchSendCoroutine = StartCoroutine(DebounceSend());
                }
            }
            else
            {
                Debug.LogWarning("[PotentialService] Lỗi đồng bộ cộng điểm! Đang tải lại dữ liệu gốc...");
                ResetPendingState();

                PlayerStatsService.Instance.SyncProfile((s) => {
                    OnPendingPointsChanged?.Invoke();
                });
            }
        });
    }

    public void RequestResetStats(Action<bool> onComplete)
    {
        if (batchSendCoroutine != null) StopCoroutine(batchSendCoroutine);
        ResetPendingState();

        PlayerStatsService.Instance.ResetStats(onComplete);
    }
}
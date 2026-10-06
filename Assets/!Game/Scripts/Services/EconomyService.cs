using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyService : MonoBehaviour
{
    public static EconomyService Instance { get; private set; }

    private ProfileRepository profileRepo;
    private readonly Queue<Action> transactionQueue = new Queue<Action>();
    private bool isProcessing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        profileRepo = new ProfileRepository();
    }

    private string CurrentProfileId => PlayerPrefs.GetString("CurrentProfileId", "");

    public void SpendCurrency(string type, int amount, string reason, Action<bool> onComplete)
    {
        transactionQueue.Enqueue(() => ProcessSpend(type, amount, reason, onComplete));
        ProcessNext();
    }

    public void EarnCurrency(string type, int amount, string reason, Action<bool> onComplete)
    {
        transactionQueue.Enqueue(() => ProcessEarn(type, amount, reason, onComplete));
        ProcessNext();
    }

    private void ProcessNext()
    {
        if (isProcessing || transactionQueue.Count == 0) return;

        isProcessing = true;
        Action nextTransaction = transactionQueue.Dequeue();
        nextTransaction?.Invoke();
        isProcessing = false;

        if (transactionQueue.Count > 0)
        {
            ProcessNext();
        }
    }

    private void ProcessSpend(string type, int amount, string reason, Action<bool> onComplete)
    {
        if (amount <= 0)
        {
            onComplete?.Invoke(false);
            return;
        }

        ProfileEntity profile = profileRepo.GetProfile(CurrentProfileId);
        if (profile == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        bool isCoin = (type == "Coin" || type == "Coins");
        bool success = false;

        if (isCoin && profile.Coin >= amount)
        {
            profile.Coin -= amount;
            success = true;
        }
        else if (!isCoin && profile.Gem >= amount)
        {
            profile.Gem -= amount;
            success = true;
        }

        if (success)
        {
            profileRepo.UpdateProfile(profile);
            if (isCoin) PlayerWallet.Instance.SyncCoinFromServer(profile.Coin);
            else PlayerWallet.Instance.SyncGemFromServer(profile.Gem);
        }

        onComplete?.Invoke(success);
    }

    private void ProcessEarn(string type, int amount, string reason, Action<bool> onComplete)
    {
        if (amount <= 0)
        {
            onComplete?.Invoke(false);
            return;
        }

        ProfileEntity profile = profileRepo.GetProfile(CurrentProfileId);
        if (profile == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        bool isCoin = (type == "Coin" || type == "Coins");

        if (isCoin) profile.Coin += amount;
        else profile.Gem += amount;

        profileRepo.UpdateProfile(profile);

        if (isCoin) PlayerWallet.Instance.SyncCoinFromServer(profile.Coin);
        else PlayerWallet.Instance.SyncGemFromServer(profile.Gem);

        onComplete?.Invoke(true);
    }

    public void RefreshBalance()
    {
        ProfileEntity profile = profileRepo.GetProfile(CurrentProfileId);
        if (profile != null && PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.SyncFromServer(profile.Coin, profile.Gem);
        }
    }
}
using System;
using System.Collections;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public event Action OnWalletUpdated;

    [Header("Currency (View-Only)")]
    public int coin { get; private set; }
    public int gem { get; private set; }

    [Header("Network Optimization")]
    private int pendingCoinToAdd = 0;
    private Coroutine coinBatchCoroutine;
    private float coinDebounceTime = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SyncFromServer(int serverCoin, int serverGem)
    {
        coin = serverCoin;
        gem = serverGem;
        OnWalletUpdated?.Invoke();
    }

    public void SyncCoinFromServer(int serverCoin)
    {
        coin = serverCoin;
        OnWalletUpdated?.Invoke();
    }

    public void SyncGemFromServer(int serverGem)
    {
        gem = serverGem;
        OnWalletUpdated?.Invoke();
    }

    // Thêm hàm AddCoin gom nhóm tương tự AddEXP của PlayerStats
    public void AddCoin(int amount)
    {
        coin += amount;
        pendingCoinToAdd += amount;
        OnWalletUpdated?.Invoke();

        if (coinBatchCoroutine != null) StopCoroutine(coinBatchCoroutine);
        coinBatchCoroutine = StartCoroutine(SendCoinBatchRoutine());
    }

    public void ForceSyncCoinImmediate()
    {
        if (coinBatchCoroutine != null) StopCoroutine(coinBatchCoroutine);
        if (pendingCoinToAdd != 0)
        {
            if (EconomyService.Instance != null)
                EconomyService.Instance.EarnCurrency("Coin", pendingCoinToAdd, "Batch Sync", null);
            pendingCoinToAdd = 0;
        }
    }

    private IEnumerator SendCoinBatchRoutine()
    {
        yield return new WaitForSeconds(coinDebounceTime);
        if (pendingCoinToAdd > 0)
        {
            int amountToSend = pendingCoinToAdd;
            pendingCoinToAdd = 0;
            if (EconomyService.Instance != null)
                EconomyService.Instance.EarnCurrency("Coin", amountToSend, "Batch Sync", null);
        }
    }

    public void RequestSpendCoin(int amount, string reason, Action onSuccess, Action onFail)
    {
        if (coin < amount)
        {
            GameNotify.Show("Không đủ vàng!");
            onFail?.Invoke();
            return;
        }

        EconomyService.Instance.SpendCurrency("Coin", amount, reason, (isSuccess) =>
        {
            if (isSuccess) onSuccess?.Invoke();
            else onFail?.Invoke();
        });
    }

    public void RequestSpendGem(int amount, string reason, Action onSuccess, Action onFail)
    {
        if (gem < amount)
        {
            GameNotify.Show("Không đủ Kim cương!");
            onFail?.Invoke();
            return;
        }

        EconomyService.Instance.SpendCurrency("Gem", amount, reason, (isSuccess) =>
        {
            if (isSuccess) onSuccess?.Invoke();
            else onFail?.Invoke();
        });
    }

    public void RequestAddCoin(int amount, string reason)
    {
        AddCoin(amount);
    }

    public void RequestAddGem(int amount, string reason)
    {
        EconomyService.Instance.EarnCurrency("Gem", amount, reason, null);
    }
}
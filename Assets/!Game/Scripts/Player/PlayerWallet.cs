using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public event Action OnWalletUpdated;

    [Header("Currency (View-Only)")]
    public int coin { get; private set; }
    public int gem { get; private set; }

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
        // Có thể cộng tạm View ở đây cho mượt UI nếu muốn: coin += amount; OnWalletUpdated?.Invoke();
        EconomyService.Instance.EarnCurrency("Coin", amount, reason, null);
    }

    public void RequestAddGem(int amount, string reason)
    {
        EconomyService.Instance.EarnCurrency("Gem", amount, reason, null);
    }
}
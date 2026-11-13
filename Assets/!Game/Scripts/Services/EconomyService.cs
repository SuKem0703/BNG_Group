using System;
using UnityEngine;

public class EconomyService : MonoBehaviour
{
    public static EconomyService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Luôn dùng định danh được DatabaseManager chọn cho instance hiện tại.
    private string CurrentProfileId => DatabaseManager.Instance != null
        ? DatabaseManager.Instance.GetActiveProfileId() : "";

    private static bool TryGetCurrencyColumn(string type, out string columnName, out bool isCoin)
    {
        isCoin = type == "Coin" || type == "Coins";
        if (isCoin)
        {
            columnName = "Coin";
            return true;
        }

        if (type == "Gem" || type == "Gems")
        {
            columnName = "Gem";
            return true;
        }

        columnName = null;
        return false;
    }

    public void SpendCurrency(string type, int amount, string reason, Action<bool> onComplete)
    {
        string profileId = CurrentProfileId;
        if (amount <= 0 || DatabaseManager.Instance == null || string.IsNullOrEmpty(profileId)
            || !TryGetCurrencyColumn(type, out string columnName, out bool isCoin))
        {
            onComplete?.Invoke(false);
            return;
        }

        var db = DatabaseManager.Instance.GetConnection();
        int newBalance = 0;
        bool success = false;

        db.RunInTransaction(() =>
        {
            // Chỉ trừ khi profile tồn tại và có đủ số dư.
            int affected = db.Execute(
                $"UPDATE Profiles SET {columnName} = {columnName} - ? " +
                $"WHERE ProfileId = ? AND {columnName} >= ?",
                amount, profileId, amount);

            if (affected == 1)
            {
                newBalance = db.ExecuteScalar<int>(
                    $"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);
                success = true;
            }
        });

        if (success && PlayerWallet.Instance != null)
        {
            if (isCoin) PlayerWallet.Instance.SyncCoinFromServer(newBalance);
            else PlayerWallet.Instance.SyncGemFromServer(newBalance);
        }

        onComplete?.Invoke(success);
    }

    public void EarnCurrency(string type, int amount, string reason, Action<bool> onComplete)
    {
        string profileId = CurrentProfileId;
        if (amount <= 0 || DatabaseManager.Instance == null || string.IsNullOrEmpty(profileId)
            || !TryGetCurrencyColumn(type, out string columnName, out bool isCoin))
        {
            onComplete?.Invoke(false);
            return;
        }

        var db = DatabaseManager.Instance.GetConnection();
        int newBalance = 0;
        bool success = false;

        db.RunInTransaction(() =>
        {
            int affected = db.Execute(
                $"UPDATE Profiles SET {columnName} = {columnName} + ? WHERE ProfileId = ?",
                amount, profileId);

            if (affected == 1)
            {
                newBalance = db.ExecuteScalar<int>(
                    $"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);
                success = true;
            }
        });

        if (success && PlayerWallet.Instance != null)
        {
            if (isCoin) PlayerWallet.Instance.SyncCoinFromServer(newBalance);
            else PlayerWallet.Instance.SyncGemFromServer(newBalance);
        }

        onComplete?.Invoke(success);
    }

    public void RefreshBalance()
    {
        if (DatabaseManager.Instance == null) return;

        string profileId = CurrentProfileId;
        if (string.IsNullOrEmpty(profileId)) return;

        var db = DatabaseManager.Instance.GetConnection();
        var profile = db.Table<ProfileEntity>()
            .FirstOrDefault(p => p.ProfileId == profileId);

        if (profile != null && PlayerWallet.Instance != null)
            PlayerWallet.Instance.SyncFromServer(profile.Coin, profile.Gem);
    }
}

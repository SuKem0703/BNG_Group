using System;
using UnityEngine;

public class EconomyService : MonoBehaviour
{
    public static EconomyService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private string CurrentProfileId
    {
        get
        {
            string id = PlayerPrefs.GetString("CurrentProfileId", "");
            return string.IsNullOrEmpty(id) ? "OfflineUser" : id;
        }
    }

    public void SpendCurrency(string type, int amount, string reason, Action<bool> onComplete)
    {
        if (amount <= 0 || DatabaseManager.Instance == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        var db = DatabaseManager.Instance.GetConnection();
        string profileId = CurrentProfileId;

        bool isCoin = (type == "Coin" || type == "Coins");
        string columnName = isCoin ? "Coin" : "Gem";

        bool success = false;
        int newBalance = 0;

        // Đưa vào Transaction để khóa dòng, tránh xung đột đồng thời
        db.RunInTransaction(() =>
        {
            int currentBalance = db.ExecuteScalar<int>($"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);

            if (currentBalance >= amount)
            {
                db.Execute($"UPDATE Profiles SET {columnName} = {columnName} - ? WHERE ProfileId = ?", amount, profileId);
                newBalance = db.ExecuteScalar<int>($"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);
                success = true;
            }
        });

        if (success)
        {
            if (isCoin && PlayerWallet.Instance != null) PlayerWallet.Instance.SyncCoinFromServer(newBalance);
            else if (!isCoin && PlayerWallet.Instance != null) PlayerWallet.Instance.SyncGemFromServer(newBalance);

            onComplete?.Invoke(true);
        }
        else
        {
            onComplete?.Invoke(false);
        }
    }

    public void EarnCurrency(string type, int amount, string reason, Action<bool> onComplete)
    {
        if (amount <= 0 || DatabaseManager.Instance == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        var db = DatabaseManager.Instance.GetConnection();
        string profileId = CurrentProfileId;

        bool isCoin = (type == "Coin" || type == "Coins");
        string columnName = isCoin ? "Coin" : "Gem";

        int newBalance = 0;

        // Bọc toàn bộ thao tác Đọc - Ghi trong Transaction an toàn
        db.RunInTransaction(() =>
        {
            db.Execute($"UPDATE Profiles SET {columnName} = {columnName} + ? WHERE ProfileId = ?", amount, profileId);
            newBalance = db.ExecuteScalar<int>($"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);
        });

        if (isCoin && PlayerWallet.Instance != null)
            PlayerWallet.Instance.SyncCoinFromServer(newBalance);
        else if (!isCoin && PlayerWallet.Instance != null)
            PlayerWallet.Instance.SyncGemFromServer(newBalance);

        onComplete?.Invoke(true);
    }

    public void RefreshBalance()
    {
        if (DatabaseManager.Instance == null) return;

        var db = DatabaseManager.Instance.GetConnection();
        string profileId = CurrentProfileId;

        var profile = db.Table<ProfileEntity>().FirstOrDefault(p => p.ProfileId == profileId);
        if (profile != null && PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.SyncFromServer(profile.Coin, profile.Gem);
        }
    }
}
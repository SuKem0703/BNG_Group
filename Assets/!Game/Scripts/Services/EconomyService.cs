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

        db.RunInTransaction(() =>
        {
            int currentBalance = db.ExecuteScalar<int>($"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);

            if (currentBalance >= amount)
            {
                db.Execute($"UPDATE Profiles SET {columnName} = {columnName} - ? WHERE ProfileId = ?", amount, profileId);

                int newBalance = currentBalance - amount;

                if (isCoin) PlayerWallet.Instance.SyncCoinFromServer(newBalance);
                else PlayerWallet.Instance.SyncGemFromServer(newBalance);

                onComplete?.Invoke(true);
            }
            else
            {
                onComplete?.Invoke(false);
            }
        });
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

        db.Execute($"UPDATE Profiles SET {columnName} = {columnName} + ? WHERE ProfileId = ?", amount, profileId);

        int newBalance = db.ExecuteScalar<int>($"SELECT {columnName} FROM Profiles WHERE ProfileId = ?", profileId);

        if (isCoin) PlayerWallet.Instance.SyncCoinFromServer(newBalance);
        else PlayerWallet.Instance.SyncGemFromServer(newBalance);

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
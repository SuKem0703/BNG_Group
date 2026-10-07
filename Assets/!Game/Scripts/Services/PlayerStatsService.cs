using System.Collections;
using UnityEngine;

public class PlayerStatsService : MonoBehaviour
{
    public static PlayerStatsService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    [System.Serializable]
    public class ServerUserStat
    {
        public int level;
        public int exp;
        public int potentialPoints;
        public int str;
        public int dex;
        public int intStat;
        public int con;
    }

    public void SyncProfile(System.Action<bool> onComplete = null)
    {
        StartCoroutine(SyncRoutine(onComplete));
    }

    private IEnumerator SyncRoutine(System.Action<bool> onComplete)
    {
        if (DatabaseManager.Instance != null)
        {
            string profileId = PlayerPrefs.GetString("CurrentProfileId", "");
            if (string.IsNullOrEmpty(profileId)) profileId = "OfflineUser";

            var db = DatabaseManager.Instance.GetConnection();
            var profile = db.Table<ProfileEntity>().FirstOrDefault(p => p.ProfileId == profileId);
            if (profile != null)
            {
                var data = new ServerUserStat
                {
                    level = profile.Level,
                    exp = profile.Exp,
                    potentialPoints = profile.PotentialPoints,
                    str = profile.Str,
                    dex = profile.Dex,
                    intStat = profile.IntStat,
                    con = profile.Con
                };

                if (PlayerStats.Instance != null)
                    PlayerStats.Instance.SyncStatsFromServer(data);

                onComplete?.Invoke(true);
                yield break;
            }
            else
            {
                Debug.LogWarning("[PlayerStatsService] No local profile found to sync.");
                onComplete?.Invoke(false);
                yield break;
            }
        }

        onComplete?.Invoke(false);
        yield break;
    }

    public void AddExp(int amount)
    {
        StartCoroutine(AddExpRoutine(amount));
    }

    private IEnumerator AddExpRoutine(int amount)
    {
        if (DatabaseManager.Instance != null)
        {
            string profileId = PlayerPrefs.GetString("CurrentProfileId", "");
            if (string.IsNullOrEmpty(profileId)) profileId = "OfflineUser";

            var db = DatabaseManager.Instance.GetConnection();
            var profile = db.Table<ProfileEntity>().FirstOrDefault(p => p.ProfileId == profileId);

            if (profile != null)
            {
                int oldLevel = profile.Level;

                profile.Exp += amount;

                bool leveledUp = false;
                while (true)
                {
                    int requiredExp = CalculateExpForLevel(profile.Level);
                    if (profile.Exp >= requiredExp && profile.Level < 200)
                    {
                        profile.Exp -= requiredExp;
                        profile.Level++;
                        profile.PotentialPoints += 5;
                        leveledUp = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (PlayerStats.Instance != null)
                {
                    profile.Str = PlayerStats.Instance.STR;
                    profile.Dex = PlayerStats.Instance.DEX;
                    profile.IntStat = PlayerStats.Instance.INT;
                    profile.Con = PlayerStats.Instance.CON;
                }

                db.Update(profile);

                var newStats = new ServerUserStat
                {
                    level = profile.Level,
                    exp = profile.Exp,
                    potentialPoints = profile.PotentialPoints,
                    str = profile.Str,
                    dex = profile.Dex,
                    intStat = profile.IntStat,
                    con = profile.Con
                };

                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.SyncStatsFromServer(newStats);
                    if (leveledUp) PlayerStats.Instance.PlayLevelUpEffect();
                }

                yield break;
            }
            else
            {
                Debug.LogWarning("[PlayerStatsService] No local profile found to write EXP.");
                yield break;
            }
        }
        yield break;
    }

    private int CalculateExpForLevel(int lvl)
    {
        if (lvl < 100) return Mathf.FloorToInt(100 + lvl * 50 + Mathf.Pow(lvl, 2.2f));
        else if (100 <= lvl && lvl < 200) return Mathf.FloorToInt(100 + lvl * 80 + Mathf.Pow(lvl, 2.5f));
        else return Mathf.FloorToInt(100 + lvl * 100 + Mathf.Pow(lvl, 3f));
    }

    public void DistributePoints(int addStr, int addDex, int addInt, int addCon, System.Action<bool> onComplete)
    {
        StartCoroutine(DistributeRoutine(addStr, addDex, addInt, addCon, onComplete));
    }

    private IEnumerator DistributeRoutine(int addStr, int addDex, int addInt, int addCon, System.Action<bool> onComplete)
    {
        if (DatabaseManager.Instance == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string profileId = PlayerPrefs.GetString("CurrentProfileId", "");
        if (string.IsNullOrEmpty(profileId)) profileId = "OfflineUser";

        var db = DatabaseManager.Instance.GetConnection();
        var profile = db.Table<ProfileEntity>().FirstOrDefault(p => p.ProfileId == profileId);

        if (profile == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        if (addStr < 0 || addDex < 0 || addInt < 0 || addCon < 0)
        {
            Debug.LogWarning("[PlayerStatsService] Số điểm cộng không hợp lệ.");
            onComplete?.Invoke(false);
            yield break;
        }

        int totalCost = addStr + addDex + addInt + addCon;
        if (totalCost == 0)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        if (profile.PotentialPoints < totalCost)
        {
            Debug.LogWarning("[PlayerStatsService] Không đủ điểm tiềm năng.");
            onComplete?.Invoke(false);
            yield break;
        }

        int maxAllowedPoints = 5 + (profile.Level - 1) * 5;
        int currentTotalPoints = profile.Str + profile.Dex + profile.IntStat + profile.Con + profile.PotentialPoints;

        if (currentTotalPoints > maxAllowedPoints)
        {
            Debug.LogWarning("[PlayerStatsService] Phát hiện bất thường trong dữ liệu nhân vật! Vượt giới hạn điểm.");
            onComplete?.Invoke(false);
            yield break;
        }

        profile.Str += addStr;
        profile.Dex += addDex;
        profile.IntStat += addInt;
        profile.Con += addCon;
        profile.PotentialPoints -= totalCost;

        db.Update(profile);

        var newData = new ServerUserStat
        {
            level = profile.Level,
            exp = profile.Exp,
            potentialPoints = profile.PotentialPoints,
            str = profile.Str,
            dex = profile.Dex,
            intStat = profile.IntStat,
            con = profile.Con
        };

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.SyncStatsFromServer(newData);

        onComplete?.Invoke(true);
        yield return null;
    }

    public void ResetStats(System.Action<bool> onComplete)
    {
        StartCoroutine(ResetStatsRoutine(onComplete));
    }

    private IEnumerator ResetStatsRoutine(System.Action<bool> onComplete)
    {
        if (DatabaseManager.Instance == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string profileId = PlayerPrefs.GetString("CurrentProfileId", "");
        if (string.IsNullOrEmpty(profileId)) profileId = "OfflineUser";

        var db = DatabaseManager.Instance.GetConnection();
        var profile = db.Table<ProfileEntity>().FirstOrDefault(p => p.ProfileId == profileId);

        if (profile == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        db.RunInTransaction(() =>
        {
            if (profile.Gem < 20)
            {
                Debug.LogWarning("[PlayerStatsService] Không đủ Gem (Cần 20) để tẩy điểm.");
                onComplete?.Invoke(false);
                return;
            }

            profile.Gem -= 20;

            profile.Str = 0;
            profile.Dex = 0;
            profile.IntStat = 0;
            profile.Con = 0;
            profile.PotentialPoints = 5 + (profile.Level - 1) * 5;

            db.Update(profile);

            var newStats = new ServerUserStat
            {
                level = profile.Level,
                exp = profile.Exp,
                potentialPoints = profile.PotentialPoints,
                str = profile.Str,
                dex = profile.Dex,
                intStat = profile.IntStat,
                con = profile.Con
            };

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.SyncStatsFromServer(newStats);
            }

            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.SyncGemFromServer(profile.Gem);
            }

            onComplete?.Invoke(true);
        });

        yield return null;
    }
}
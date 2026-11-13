using System.IO;
using SQLite;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }
    private SQLiteConnection connection;

    // Chỉ Editor mới ép chọn profile theo thứ tự slot (Player1 => slot 1 ...).
    // Đổi thành false để dùng luồng chọn nhân vật Main Menu trong Editor.
    private const bool EnableDevProfileAutoSelect = true;

    private string devProfileId;

    public string GetActiveProfileId()
    {
#if UNITY_EDITOR
        if (EnableDevProfileAutoSelect)
            return devProfileId ?? "";
#endif
        return PlayerPrefs.GetString("CurrentProfileId", "");
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDatabase();
            SelectDevProfileEarly();
        }
    }

    private void InitializeDatabase()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "gamedata.db");
        connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

        connection.CreateTable<ProfileEntity>();
        connection.CreateTable<InventoryItemEntity>();
    }

    private void SelectDevProfileEarly()
    {
#if UNITY_EDITOR
        if (!EnableDevProfileAutoSelect)
            return;

        int slot = DevClientIdentity.GetPlayerIndex();

        try
        {
            ProfileEntity profile = new ProfileRepository().GetProfileByTestSlot(slot);
            if (profile == null)
            {
                devProfileId = null;
                Debug.LogError($"[DevProfile] Player {slot}: không có profile ở slot {slot}. " +
                               $"Hãy kiểm tra database {Application.persistentDataPath}/gamedata.db");
                return;
            }

            devProfileId = profile.ProfileId;

            // Tương thích các Service hiện tại còn đọc CurrentProfileId từ PlayerPrefs.
            // Dữ liệu lựa chọn của SaveController được cố định riêng ở devProfileId.
            PlayerPrefs.SetString("CurrentProfileId", devProfileId);
            PlayerPrefs.Save();

            Debug.Log($"[DevProfile] Player {slot} -> {profile.PlayerName} " +
                      $"({devProfileId}) | DB: {Application.persistentDataPath}/gamedata.db");
        }
        catch (System.Exception ex)
        {
            devProfileId = null;
            Debug.LogError($"[DevProfile] Không thể chọn profile: {ex}");
        }
#endif
    }

    public SQLiteConnection GetConnection()
    {
        return connection;
    }

    private void OnApplicationQuit()
    {
        if (connection != null)
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
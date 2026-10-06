using System.IO;
using SQLite;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }
    private SQLiteConnection connection;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDatabase();
        }
    }

    private void InitializeDatabase()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "gamedata.db");
        connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

        connection.CreateTable<ProfileEntity>();
        connection.CreateTable<InventoryItemEntity>();
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
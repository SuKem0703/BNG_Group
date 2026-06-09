using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UniqueEnemySpawner : MonoBehaviour
{
    [Header("Cấu hình Boss/Quest Enemy")]
    public GameObject uniqueEnemyPrefab;
    
    [Tooltip("ID duy nhất để lưu vào SaveController. Phải đảm bảo không trùng lặp.")]
    public string uniqueID; 

    private void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (SaveController.IsDataLoaded)
        {
            CheckAndSpawn();
        }
        else
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        CheckAndSpawn();
    }

    private void CheckAndSpawn()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (SaveController.Instance.IsCollected(sceneName, uniqueID))
        {
            Destroy(gameObject);
            return;
        }

        GameObject enemyObj = Instantiate(uniqueEnemyPrefab, transform.position, Quaternion.identity);

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.isQuestEnemy = true;
            enemyScript.UniqueID = uniqueID;
        }

        var netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn(true);
        }
        
        Destroy(gameObject); 
    }

    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }
}
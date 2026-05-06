using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawnArea : MonoBehaviour
{
    [Header("Cấu hình Spawn")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int maxEnemies = 3;
    [SerializeField] private float respawnDelay = 10f;

    [SerializeField] private List<GameObject> activeEnemies = new List<GameObject>();
    private BoxCollider2D spawnBounds;

    void Awake()
    {
        spawnBounds = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        StartCoroutine(InitialSpawnRoutine());
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                var netObj = enemy.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }
        }
        activeEnemies.Clear();
    }

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < maxEnemies; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (enemyPrefabs.Length == 0 || spawnBounds == null) return;

        Vector2 spawnPos = GetRandomPointInBounds();
        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemyObj = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

        var netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(true);
        }

        activeEnemies.Add(enemyObj);

        var enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            StartCoroutine(TrackEnemyDeath(enemyObj));
        }
    }

    private Vector2 GetRandomPointInBounds()
    {
        Bounds bounds = spawnBounds.bounds;
        return new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y)
        );
    }

    private IEnumerator TrackEnemyDeath(GameObject enemyObj)
    {
        while (enemyObj != null && enemyObj.activeInHierarchy)
        {
            yield return new WaitForSeconds(1f);
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) yield break;

        activeEnemies.Remove(enemyObj);

        yield return new WaitForSeconds(respawnDelay);

        if (this != null && gameObject.activeInHierarchy)
        {
            SpawnEnemy();
        }
    }
}
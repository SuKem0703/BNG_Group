using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnArea : MonoBehaviour
{
    [Header("Cấu hình Spawn")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int maxEnemies = 3;
    [SerializeField] private float respawnDelay = 90f;

    [SerializeField] private List<GameObject> pooledEnemies = new List<GameObject>();
    private BoxCollider2D spawnBounds;

    void Awake()
    {
        spawnBounds = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening)
        {
            StartCoroutine(InitialSpawnRoutine());
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnServerStopped += HandleServerStopped;
        }
    }

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(InitialSpawnRoutine());
        }
    }

    private void HandleServerStopped(bool obj)
    {
        StopAllCoroutines();
        pooledEnemies.Clear();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleServerStopped;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (var enemy in pooledEnemies)
        {
            if (enemy != null)
            {
                var netObj = enemy.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
                else Destroy(enemy);
            }
        }
        pooledEnemies.Clear();
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

        Vector2 randomPos = GetRandomPointInBounds();
        
        Vector3 spawnPos = new Vector3(randomPos.x, randomPos.y, transform.position.z);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 3.0f, NavMesh.AllAreas))
        {
            spawnPos = hit.position; 
        }
        else
        {
            return; 
        }

        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemyObj = null;

        pooledEnemies.RemoveAll(item => item == null);

        foreach (var obj in pooledEnemies)
        {
            if (!obj.activeInHierarchy && obj.name.StartsWith(selectedPrefab.name))
            {
                enemyObj = obj;
                break;
            }
        }

        if (enemyObj == null)
        {
            enemyObj = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            enemyObj.name = selectedPrefab.name + "_" + pooledEnemies.Count;
            pooledEnemies.Add(enemyObj);
        }
        else
        {
            var agent = enemyObj.GetComponent<NavMeshAgent>();
            
            if (agent != null) agent.enabled = false; 
            
            enemyObj.transform.position = spawnPos;
            
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(spawnPos);
            }
            
            enemyObj.SetActive(true);
        }

        var enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.ResetEnemyState();
            enemyScript.SetSpawnArea(this); 
        }

        var detectionScript = enemyObj.GetComponentInChildren<EnemyDetection>();
        if (detectionScript != null)
        {
            detectionScript.SetSpawnOrigin(spawnPos);
        }

        var netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn(true);
        }

        StartCoroutine(TrackEnemyDeath(enemyObj));
    }

    private Vector2 GetRandomPointInBounds()
    {
        Bounds bounds = spawnBounds.bounds;
        return new Vector2(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y)
        );
    }

    public Vector2 GetValidPatrolPoint()
    {
        Bounds bounds = spawnBounds.bounds;
        
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomPoint = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position; 
            }
        }

        return transform.position; 
    }

    private IEnumerator TrackEnemyDeath(GameObject enemyObj)
    {
        while (enemyObj != null && enemyObj.activeInHierarchy)
        {
            yield return new WaitForSeconds(1f);
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) yield break;

        yield return new WaitForSeconds(respawnDelay);

        if (this != null && gameObject.activeInHierarchy)
        {
            SpawnEnemy();
        }
    }

    public void AlertEcosystem(Transform targetPlayer, Enemy caller)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        foreach (var enemyObj in pooledEnemies)
        {
            if (enemyObj != null && enemyObj.activeInHierarchy)
            {
                var allyEnemy = enemyObj.GetComponent<Enemy>();
                if (allyEnemy != null && allyEnemy != caller && !allyEnemy.isDead)
                {
                    var ai = allyEnemy.GetComponent<EnemyCombatAI>();
                    if (ai != null)
                    {
                        ai.OnPlayerDetected(targetPlayer);
                    }
                }
            }
        }
    }
}
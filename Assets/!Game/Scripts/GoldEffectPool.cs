using System.Collections.Generic;
using UnityEngine;

public class GoldEffectPool : MonoBehaviour
{
    public static GoldEffectPool Instance { get; private set; }

    [Header("Settings")]
    public GoldVisualEffect goldPrefab;
    public int initialPoolSize = 10;

    private Queue<GoldVisualEffect> pool = new Queue<GoldVisualEffect>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewGold();
        }
    }

    private GoldVisualEffect CreateNewGold()
    {
        GoldVisualEffect gold = Instantiate(goldPrefab, transform);
        gold.gameObject.SetActive(false);
        gold.OnGoldCollected = ReturnToPool;
        pool.Enqueue(gold);
        return gold;
    }

    public void SpawnGold(Vector3 position, int amount)
    {
        if (pool.Count == 0)
        {
            CreateNewGold();
        }

        GoldVisualEffect gold = pool.Dequeue();
        gold.Spawn(position, amount);
    }

    private void ReturnToPool(GoldVisualEffect gold)
    {
        pool.Enqueue(gold);
    }
}
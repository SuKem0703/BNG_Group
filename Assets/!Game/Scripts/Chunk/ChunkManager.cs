using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [Header("Settings")]
    public Transform player;
    public float chunkSize = 16f;

    public int loadDistance = 1;
    public int unloadDistance = 2;
    public int objectsDestroyedPerFrame = 5;

    public string savePath = "Assets/!Game/Resources/ChunkData/";

    private string currentSceneName;

    private Dictionary<Vector2Int, ChunkData> allChunkData = new Dictionary<Vector2Int, ChunkData>();
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();

    private Coroutine chunkTickCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentSceneName = SceneManager.GetActiveScene().name;
        allChunkData.Clear();
        activeChunks.Clear();
    }

    private void OnEnable()
    {
        PlayerStats.OnInitialized += OnLocalPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerStats.OnInitialized -= OnLocalPlayerSpawned;
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            InitPlayerTracking(PlayerStats.Instance.transform);
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("PlayerController");
            if (pObj != null)
            {
                InitPlayerTracking(pObj.transform);
                return;
            }
        }
    }

    private void OnLocalPlayerSpawned(PlayerStats stats)
    {
        InitPlayerTracking(stats.transform);
    }

    private void InitPlayerTracking(Transform localPlayer)
    {
        player = localPlayer;
        UpdateChunks();

        if (chunkTickCoroutine != null) StopCoroutine(chunkTickCoroutine);
        chunkTickCoroutine = StartCoroutine(ChunkUpdateTick());
    }

    private IEnumerator ChunkUpdateTick()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);

        while (true)
        {
            UpdateChunks();
            yield return waitTime;
        }
    }

    public Vector2Int WorldToGrid(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSize);
        int y = Mathf.FloorToInt(pos.y / chunkSize);
        return new Vector2Int(x, y);
    }

    private void UpdateChunks()
    {
        HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        HashSet<Vector2Int> keepAliveChunks = new HashSet<Vector2Int>();

        List<Transform> playersToTrack = new List<Transform>();

        if (player != null) playersToTrack.Add(player);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null && client.PlayerObject.transform != player)
                {
                    playersToTrack.Add(client.PlayerObject.transform);
                }
            }
        }

        if (playersToTrack.Count == 0) return;

        foreach (var p in playersToTrack)
        {
            Vector2Int center = WorldToGrid(p.position);

            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    requiredChunks.Add(new Vector2Int(center.x + x, center.y + y));
                }
            }

            for (int x = -unloadDistance; x <= unloadDistance; x++)
            {
                for (int y = -unloadDistance; y <= unloadDistance; y++)
                {
                    keepAliveChunks.Add(new Vector2Int(center.x + x, center.y + y));
                }
            }
        }

        foreach (var coord in requiredChunks)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                LoadChunk(coord);
            }
        }

        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var coord in activeChunks.Keys)
        {
            if (!keepAliveChunks.Contains(coord))
            {
                chunksToRemove.Add(coord);
            }
        }

        foreach (var coord in chunksToRemove)
        {
            UnloadChunk(coord);
        }
    }

    private void LoadChunk(Vector2Int coord)
    {
        GameObject chunkGO = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkGO.transform.position = new Vector3(coord.x * chunkSize, coord.y * chunkSize, 0);
        chunkGO.transform.SetParent(transform);

        activeChunks[coord] = chunkGO;
        ChunkData data = null;

        if (allChunkData.TryGetValue(coord, out ChunkData cachedData))
        {
            data = cachedData;
        }
        else
        {
            string finalSavePath = Path.Combine(savePath, currentSceneName);
            string filePath = Path.Combine(finalSavePath, $"Chunk_{coord.x}_{coord.y}.json");

            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                data = JsonUtility.FromJson<ChunkData>(jsonContent);
            }
            else
            {
                data = new ChunkData { coord = coord, entities = new List<EntitySaveData>() };
            }
            allChunkData[coord] = data;
        }

        for (int i = 0; i < data.entities.Count; i++)
        {
            var entity = data.entities[i];
            if (string.IsNullOrEmpty(entity.entityID)) continue;
            GameObject prefab = WorldObjectDictionary.Instance.GetPrefab(entity.entityID);

            if (prefab != null)
            {
                GameObject spawnedObj = Instantiate(prefab, entity.position, Quaternion.identity, chunkGO.transform);

                string deterministicID = GlobalHelper.GenerateUniqueID(spawnedObj);

                var marker = spawnedObj.GetComponent<ChunkEntityMarker>();
                if (marker != null)
                {
                    marker.prefabID = entity.entityID;
                    marker.entityType = entity.entityType;
                }

                SetupEntityLogic(spawnedObj, entity, deterministicID);

                var dynamicSort = spawnedObj.GetComponent<DynamicSorting>();
                if (dynamicSort != null)
                {
                    dynamicSort.InitSorting(entity.sortingBuffer);
                }
                else
                {
                    var spriteSort = spawnedObj.GetComponent<SpriteDynamicSorting>();
                    if (spriteSort != null)
                    {
                        spriteSort.InitSorting(entity.sortingBuffer);
                    }
                }
            }
        }
    }

    private void SetupEntityLogic(GameObject obj, EntitySaveData data, string uniqueID)
    {
        switch (data.entityType)
        {
            case EntityType.Interactable:
                var monologue = obj.AddComponent<Monologue>();
                monologue.triggerOnEnter = data.triggerOnEnter;
                monologue.isOneTimeOnly = data.isOneTimeOnly;
                monologue.uniqueID = data.uniqueID;

                if (!string.IsNullOrEmpty(data.monologueDataPath))
                {
                    monologue.monologueData = Resources.Load<MonologueData>(data.monologueDataPath);
                }

                EnsureCollider(obj, data);
                break;

            case EntityType.NPC:
                var npc = obj.GetComponent<NPC>();
                if (npc != null)
                {
                    npc.InitChunkData(uniqueID, data.triggerOnEnter);

                    if (data.npcDialoguePaths != null && data.npcDialoguePaths.Length > 0)
                    {
                        npc.dialogueDataList = new NPCDialogue[data.npcDialoguePaths.Length];
                        for (int i = 0; i < data.npcDialoguePaths.Length; i++)
                        {
                            npc.dialogueDataList[i] = Resources.Load<NPCDialogue>(data.npcDialoguePaths[i]);
                        }
                    }
                }
                var facing = obj.GetComponent<NPCAnimation>();
                if (facing != null)
                {
                    facing.initialFacing = data.npcFacing;
                }
                break;

            case EntityType.Container:
                var chest = obj.GetComponent<Chest>();
                if (chest != null)
                {
                    chest.InitChunkData(data, uniqueID);

                    chest.rarityMode = data.rarityMode;
                    chest.fixedRarity = data.fixedRarity;
                    chest.qualityMode = data.qualityMode;

                    if (!string.IsNullOrEmpty(data.rewardItemPath))
                    {
                        chest.itemPrefab = Resources.Load<GameObject>(data.rewardItemPath);
                    }
                }
                EnsureCollider(obj, data);
                break;

            case EntityType.Static:
                var storageChest = obj.GetComponent<StorageChest>();
                if (storageChest != null)
                {
                    storageChest.InitChunkData(uniqueID);
                }
                break;

            case EntityType.Enemy:
                var spawnArea = obj.GetComponent<EnemySpawnArea>();
                if (spawnArea == null)
                {
                    Debug.LogWarning($"[Chunk] Cảnh báo: {obj.name} là Enemy/Spawner nhưng thiếu EnemySpawnArea!");
                }
                break;
        }
    }

    private void EnsureCollider(GameObject obj, EntitySaveData data)
    {
        BoxCollider2D triggerCol = null;
        BoxCollider2D[] cols = obj.GetComponents<BoxCollider2D>();
        foreach (var c in cols)
        {
            if (c.isTrigger)
            {
                triggerCol = c;
                break;
            }
        }

        if (triggerCol == null)
        {
            triggerCol = obj.AddComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;
        }

        if (data.triggerSize != Vector2.zero)
        {
            triggerCol.size = data.triggerSize;
            triggerCol.offset = data.triggerOffset;
        }
    }

    private void UnloadChunk(Vector2Int coord)
    {
        if (activeChunks.TryGetValue(coord, out GameObject chunkGO))
        {
            activeChunks.Remove(coord);

            StartCoroutine(DestroyChunkRoutine(chunkGO));
        }
    }

    private IEnumerator DestroyChunkRoutine(GameObject chunkGO)
    {
        chunkGO.name += "_Unloading";
        chunkGO.SetActive(false);

        Queue<GameObject> childrenQueue = new Queue<GameObject>();
        foreach (Transform child in chunkGO.transform)
        {
            childrenQueue.Enqueue(child.gameObject);
        }

        int destroyedCount = 0;
        while (childrenQueue.Count > 0)
        {
            GameObject child = childrenQueue.Dequeue();
            if (child != null)
            {
                Destroy(child);
                destroyedCount++;

                if (destroyedCount >= objectsDestroyedPerFrame)
                {
                    destroyedCount = 0;
                    yield return null;
                }
            }
        }

        Destroy(chunkGO);
    }
}
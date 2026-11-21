
using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerCore : NetworkBehaviour
{
    public static PlayerCore Instance { get; private set; }

    public static event Action<PlayerCore> OnPlayerSpawned;
    public static event Action<PlayerCore> OnPlayerDespawned;

    [Header("Core References")]
    public PlayerStats playerStats;
    public PlayerVitals playerVitals;
    public PlayerMovement playerMovement;
    public ClassController classController;
    public PlayerVFXController playerVFX;

    [Header("Internal Controllers")]
    [SerializeField] private PlayerBuffController buffController;

    private void Awake()
    {
        if (buffController == null)
            buffController = GetComponent<PlayerBuffController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (buffController == null)
        {
            Debug.LogError(
                "[PlayerCore] Missing PlayerBuffController!",
                this
            );
        }

        if (!IsOwner)
            return;

        Instance = this;

        if (SaveController.Instance != null)
        {
            SaveController.Instance.RegisterLocalPlayer(this);
        }
        else
        {
            Debug.LogWarning(
                "[PlayerCore] SaveController chưa khởi tạo!"
            );
        }

        if (playerStats != null)
        {
            playerStats.ApplyEquippedItems();
        }

        OnPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            if (Instance == this)
                Instance = null;

            if (SaveController.Instance != null)
            {
                SaveController.Instance.UnregisterLocalPlayer();
            }

            OnPlayerDespawned?.Invoke(this);
        }

        base.OnNetworkDespawn();
    }

    // ==========================================
    // CASTING NETWORK GATEWAY
    // ==========================================

    [ServerRpc]
    public void SyncCastingServerRpc(bool casting)
    {
        SyncCastingClientRpc(casting);
    }

    [ClientRpc]
    private void SyncCastingClientRpc(bool casting)
    {
        if (IsOwner || playerVFX == null)
            return;

        if (casting)
            playerVFX.StartCastingVisual();
        else
            playerVFX.StopCastingVisual();
    }

    // ==========================================
    // BUFF GATEWAY
    // ==========================================

    public void RequestBuff(string skillID, int rank)
    {
        if (buffController == null)
        {
            Debug.LogWarning(
                "[PlayerCore] BuffController chưa được gán!",
                this
            );
            return;
        }

        buffController.RequestBuff(skillID, rank);
    }

    // ==========================================
    // TRANSFORM GATEWAY
    // ==========================================

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
        Physics2D.SyncTransforms();
    }
}

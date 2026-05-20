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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Instance = this;

            if (SaveController.Instance != null)
            {
                SaveController.Instance.RegisterLocalPlayer(this);
            }
            else
            {
                Debug.LogWarning("[PlayerCore] SaveController chưa khởi tạo, không thể đăng ký!");
            }

            OnPlayerSpawned?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            if (Instance == this) Instance = null;

            if (SaveController.Instance != null)
            {
                SaveController.Instance.UnregisterLocalPlayer();
            }

            OnPlayerDespawned?.Invoke(this);
        }

        base.OnNetworkDespawn();
    }

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
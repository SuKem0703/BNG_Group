using Unity.Netcode;
using UnityEngine;

public class LocalPlayerSaveAdapter : NetworkBehaviour
{
    public PlayerStats playerStats;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            if (SaveController.Instance != null)
            {
                SaveController.Instance.RegisterLocalPlayer(this);
            }
            else
            {
                Debug.LogWarning("[LocalPlayerSaveAdapter] SaveController chưa khởi tạo, không thể đăng ký!");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && SaveController.Instance != null)
        {
            SaveController.Instance.UnregisterLocalPlayer();
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
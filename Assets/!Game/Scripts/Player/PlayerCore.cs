
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

    [Header("Healing Buff")]
    [SerializeField, Min(0.1f)]
    private float healingBuffDuration = 10f;

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

    // Đồng bộ hoạt ảnh Casting.
    [ServerRpc]
    public void SyncCastingServerRpc(bool casting)
    {
        SyncCastingClientRpc(casting);
    }

    [ClientRpc]
    private void SyncCastingClientRpc(bool casting)
    {
        if (IsOwner)
            return;

        if (playerVFX == null)
            return;

        if (casting)
            playerVFX.StartCastingVisual();
        else
            playerVFX.StopCastingVisual();
    }

    // Owner yêu cầu server kích hoạt buff.
    [ServerRpc]
    public void RequestHealingBuffServerRpc(
        string skillID,
        int rank)
    {
        SkillData skill = SkillDictionary.Instance != null
            ? SkillDictionary.Instance.GetSkill(skillID)
            : null;

        if (skill == null ||
            skill.skillType != SkillType.Active ||
            skill.effectID != EffectService.HealRegenID ||
            rank < 1 ||
            rank > skill.maxRank ||
            playerVitals == null ||
            playerVitals.netIsDead.Value)
        {
            return;
        }

        float healFractionPerTick =
            Mathf.Clamp01(skill.GetSkillValue(rank));

        if (healFractionPerTick <= 0f)
            return;

        // Các Player đã spawn trong cùng scene.
        foreach (NetworkClient client in
                 NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            PlayerCore target =
                client.PlayerObject
                    .GetComponentInChildren<PlayerCore>(true);

            if (target == null ||
                !target.IsSpawned ||
                target.gameObject.scene != gameObject.scene ||
                target.playerVitals == null ||
                target.playerVitals.netIsDead.Value)
            {
                continue;
            }

            target.ApplyHealingBuffClientRpc(
                healingBuffDuration,
                healFractionPerTick
            );
        }
    }

    // Chạy trên mỗi client cho Player nhận buff.
    [ClientRpc]
    private void ApplyHealingBuffClientRpc(
        float duration,
        float healFractionPerTick)
    {
        if (EffectService.Instance == null)
        {
            Debug.LogError(
                "[HealingBuff] EffectService chưa tồn tại!"
            );
            return;
        }

        EffectService.Instance.AddEffect(
            gameObject,
            EffectService.HealRegenID,
            duration,
            healFractionPerTick
        );

        if (playerVFX != null)
        {
            playerVFX.ShowHealingBuffVFX(duration);
        }
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

using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class VampireKing : Enemy
{
    [Header("Vampire Stats")]
    public float lifeStealRatio = 0.5f;
    public float regenInterval = 1.0f;

    [Header("Phase Settings")]
    public float phase1Speed = 2.0f;
    public float phase2Speed = 5.0f;

    [Header("Phase 1: Summoning")]
    public GameObject minionPrefab;
    public float summonDistance = 2.0f;
    public float summonInterval = 10f;

    private bool _hitFrame1Success = false;
    private int _frame1DamageDealt = 0;
    private float _regenTimer = 0f;
    private float _lastDamageTime = -1f;
    private const float DAMAGE_EVENT_COOLDOWN = 0.01f;
    private float _lastSummonTime = -999f;

    private bool _hasExplodedOnDeath = false;
    private bool _isTrueForm = false;
    private int _lastPhaseIndex = 0;

    private List<GameObject> _activeMinions = new List<GameObject>();
    private CapsuleCollider2D _bodyCollider;

    private EnemyCombatAI _combatAI;
    private Transform TargetPlayer => _combatAI != null ? _combatAI.player : null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _bodyCollider = GetComponent<CapsuleCollider2D>();
        _combatAI = GetComponent<EnemyCombatAI>();

        if (IsServer)
        {
            if (currentPhaseIndex == 0)
            {
                _isTrueForm = false;
                chaseSpeed = phase1Speed;
            }
            else
            {
                _isTrueForm = true;
                chaseSpeed = phase2Speed;
            }

            UpdateAnimatorPhaseClientRpc(_isTrueForm);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!IsServer) return;

        if (currentPhaseIndex != _lastPhaseIndex)
        {
            _lastPhaseIndex = currentPhaseIndex;
            if (currentPhaseIndex == 1)
            {
                _isTrueForm = true;
                chaseSpeed = phase2Speed;
                Debug.Log("VAMPIRE KING: PHASE 2!");

                ClearMinions();
                UpdateAnimatorPhaseClientRpc(_isTrueForm);
            }
        }

        if (_isTrueForm)
        {
            HandlePassiveRegen();
            HandleGhostMovement();
        }
    }

    [ClientRpc]
    private void UpdateAnimatorPhaseClientRpc(bool isTrueForm)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("isTrueForm", isTrueForm);
        }
    }

    public void DealVampireDamage(int frameIndex)
    {
        if (!IsServer) return;

        if (Time.time - _lastDamageTime < DAMAGE_EVENT_COOLDOWN) return;
        _lastDamageTime = Time.time;

        if (isDead || isStunned || PauseController.IsGamePause) return;

        if (frameIndex == 1)
        {
            _hitFrame1Success = false;
            _frame1DamageDealt = 0;
        }

        Transform currentPlayer = TargetPlayer;
        if (currentPlayer == null || Vector2.Distance(transform.position, currentPlayer.position) > attackRange) return;

        var pStats = currentPlayer.GetComponentInParent<PlayerStats>();
        if (pStats == null) return;

        switch (frameIndex)
        {
            case 1:
                if (!_isTrueForm)
                {
                    if (Time.time >= _lastSummonTime + summonInterval)
                    {
                        SummonMinions();
                        _lastSummonTime = Time.time;
                    }
                }
                else
                {
                    int rawDamage = Mathf.RoundToInt(damage * 1.2f);
                    _frame1DamageDealt = pStats.TakeDamage(rawDamage);
                    _hitFrame1Success = _frame1DamageDealt > 0;
                }
                break;

            case 2:
                if (!_isTrueForm) return;
                if (!_hitFrame1Success || _frame1DamageDealt <= 0) return;

                int healAmount = Mathf.RoundToInt(_frame1DamageDealt * lifeStealRatio);
                if (healAmount > 0)
                {
                    netHealth.Value = Mathf.Min(netHealth.Value + healAmount, maxHealth);
                    ShowHealPopupClientRpc(healAmount);
                }
                break;

            case 3:
                int finisherDmg = Mathf.RoundToInt(damage * 1.5f);
                pStats.TakeDamage(finisherDmg);
                break;
        }
    }

    private void SummonMinions()
    {
        if (minionPrefab == null) return;

        _activeMinions.RemoveAll(item => item == null);

        Vector2 facingDir = Vector2.down;
        Transform currentPlayer = TargetPlayer;
        if (currentPlayer != null)
        {
            facingDir = (currentPlayer.position - transform.position).normalized;
        }

        float[] angles = { -30f, 30f };

        foreach (float angle in angles)
        {
            Vector2 spawnDirection = Quaternion.Euler(0, 0, angle) * facingDir;
            Vector3 spawnPos = transform.position + (Vector3)spawnDirection * summonDistance;

            GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);

            var netObj = minion.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
            }

            _activeMinions.Add(minion);
        }

        Debug.Log("Summoned 2 minions in triangle formation!");
    }

    private void ClearMinions()
    {
        if (_activeMinions.Count > 0)
        {
            var minionsToKill = new List<GameObject>(_activeMinions);

            foreach (var minion in minionsToKill)
            {
                if (minion != null)
                {
                    Enemy minionScript = minion.GetComponent<Enemy>();

                    if (minionScript != null)
                    {
                        minionScript.TakeDamage(99999, DamageSourceType.Environment);
                    }
                    else
                    {
                        var netObj = minion.GetComponent<NetworkObject>();
                        if (netObj != null && netObj.IsSpawned)
                        {
                            netObj.Despawn(true);
                        }
                        else
                        {
                            Destroy(minion);
                        }
                    }
                }
            }
            _activeMinions.Clear();
            Debug.Log($"Sacrificed {minionsToKill.Count} minions for Phase 2!");
        }
    }

    private void HandleGhostMovement()
    {
        if (_bodyCollider == null || TargetPlayer == null || isDead) return;
        float distance = Vector2.Distance(transform.position, TargetPlayer.position);

        bool isChasing = distance <= detectionRadius && distance > attackRange;

        if (_bodyCollider.isTrigger != isChasing) _bodyCollider.isTrigger = isChasing;
    }

    public override void DealDamage() { }

    private void HandlePassiveRegen()
    {
        if (isDead || isAttacking || isStunned) return;
        if (rb.linearVelocity.magnitude < 0.1f) return;

        if (netHealth.Value >= maxHealth * 0.7f) return;

        _regenTimer += Time.deltaTime;
        if (_regenTimer >= regenInterval)
        {
            _regenTimer = 0f;
            int healAmount = Mathf.FloorToInt(maxHealth * 0.01f);
            netHealth.Value = Mathf.Min(netHealth.Value + healAmount, maxHealth);
            ShowHealPopupClientRpc(healAmount);
        }
    }

    public void DealDamageWhenDead()
    {
        if (!IsServer || _hasExplodedOnDeath || !isDead) return;

        _hasExplodedOnDeath = true;

        Transform currentPlayer = TargetPlayer;
        if (currentPlayer != null && Vector2.Distance(transform.position, currentPlayer.position) <= attackRange)
        {
            var pStats = currentPlayer.GetComponentInParent<PlayerStats>();
            if (pStats != null)
            {
                int deathDamage = Mathf.RoundToInt(maxHealth * 0.1f);
                pStats.TakeDamage(deathDamage);
            }
        }
    }

    [ClientRpc]
    private void ShowHealPopupClientRpc(int amount)
    {
        if (amount <= 0) return;
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.DamagePopupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1.5f, 0);
            GameObject popupGO = Instantiate(LoadResourceManager.Instance.DamagePopupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(amount, DamageSourceType.Heal);
        }
    }

    public override void Die()
    {
        if (IsServer)
        {
            _activeMinions.Clear();
        }
        base.Die();
    }
}
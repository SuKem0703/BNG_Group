using UnityEngine;
using System.Collections.Generic;

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

    // Khoảng cách triệu hồi đệ
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

    private List<GameObject> _activeMinions = new List<GameObject>();

    private CapsuleCollider2D _bodyCollider;

    protected override void Start()
    {
        base.Start();

        _bodyCollider = GetComponent<CapsuleCollider2D>();

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

        UpdateAnimatorPhase();
    }

    protected override void Update()
    {
        base.Update();

        if (_isTrueForm)
        {
            HandlePassiveRegen();
            HandleGhostMovement();
        }
    }

    protected override void OnPhaseChange(int nextPhaseIndex)
    {
        base.OnPhaseChange(nextPhaseIndex);

        if (nextPhaseIndex == 1)
        {
            _isTrueForm = true;
            chaseSpeed = phase2Speed;
            Debug.Log("VAMPIRE KING: PHASE 2!");

            ClearMinions();

            UpdateAnimatorPhase();
        }
    }

    private void UpdateAnimatorPhase()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("isTrueForm", _isTrueForm);
        }
    }

    public void DealVampireDamage(int frameIndex)
    {
        if (Time.time - _lastDamageTime < DAMAGE_EVENT_COOLDOWN) return;
        _lastDamageTime = Time.time;

        if (isDead || isStunned || PauseController.IsGamePause) return;

        if (frameIndex == 1)
        {
            _hitFrame1Success = false;
            _frame1DamageDealt = 0;
        }

        if (player == null || Vector2.Distance(transform.position, player.position) > attackRange) return;

        var pStats = player.GetComponentInParent<PlayerStats>();
        if (pStats == null) return;

        switch (frameIndex)
        {
            case 1:
                if (!_isTrueForm)
                {
                    Debug.Log("[Phase 1] Frame 1: Summoning Motion");
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
                    currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
                    ShowHealPopup(healAmount);
                }
                break;

            case 3:
                int finisherDmg = Mathf.RoundToInt(damage * 1.5f);
                pStats.TakeDamage(finisherDmg);
                break;
        }
    }

    // Triệu hồi theo hình tam giác đều dựa trên hướng nhìn
    private void SummonMinions()
    {
        if (minionPrefab == null) return;

        // Dọn dẹp danh sách
        _activeMinions.RemoveAll(item => item == null);

        // 1. Xác định hướng quay mặt của Boss (hướng về phía Player)
        Vector2 facingDir = Vector2.down; // Mặc định nếu mất player
        if (player != null)
        {
            facingDir = (player.position - transform.position).normalized;
        }

        // 2. Góc lệch để tạo hình tam giác đều: +/- 30 độ so với hướng chính diện
        // Boss là đỉnh, 2 minion là 2 đỉnh còn lại
        float[] angles = { -30f, 30f };

        foreach (float angle in angles)
        {
            // Công thức xoay vector trong Unity (Quaternion * Vector)
            // Xoay hướng nhìn đi 30 độ trái/phải
            Vector2 spawnDirection = Quaternion.Euler(0, 0, angle) * facingDir;

            // Tính vị trí cuối cùng
            Vector3 spawnPos = transform.position + (Vector3)spawnDirection * summonDistance;

            // Triệu hồi
            GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            _activeMinions.Add(minion);

            // (Optional) Spawn Effect
            // Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity);
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

                        Destroy(minion, 2.0f);
                    }
                    else
                    {
                        Destroy(minion);
                    }
                }
            }
            _activeMinions.Clear();
            Debug.Log($"Sacrificed {minionsToKill.Count} minions for Phase 2!");
        }
    }

    private void HandleGhostMovement()
    {
        if (_bodyCollider == null || player == null || isDead) return;
        float distance = Vector2.Distance(transform.position, player.position);

        bool isChasing = distance <= detectionRadius && distance > attackRange;

        if (_bodyCollider.isTrigger != isChasing) _bodyCollider.isTrigger = isChasing;
    }

    public override void DealDamage() { }

    private void HandlePassiveRegen()
    {
        if (isDead || isAttacking || isStunned) return;
        if (rb.linearVelocity.magnitude < 0.1f) return;
        if (currentHealth >= maxHealth * 0.7f) return;

        _regenTimer += Time.deltaTime;
        if (_regenTimer >= regenInterval)
        {
            _regenTimer = 0f;
            int healAmount = Mathf.FloorToInt(maxHealth * 0.01f);
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            ShowHealPopup(healAmount);
        }
    }

    public void DealDamageWhenDead()
    {
        if (_hasExplodedOnDeath || !isDead) return;

        _hasExplodedOnDeath = true;

        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            var pStats = player.GetComponentInParent<PlayerStats>();
            if (pStats != null)
            {
                int deathDamage = Mathf.RoundToInt(maxHealth * 0.1f);
                pStats.TakeDamage(deathDamage);
            }
        }
    }

    private void ShowHealPopup(int amount)
    {
        if (amount <= 0) return;
        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1.5f, 0);
            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(amount, DamageSourceType.Heal);
        }
    }

    protected override void Dead()
    {
        if (!isDead) return;
        _activeMinions.Clear();
        base.Dead();
    }
}
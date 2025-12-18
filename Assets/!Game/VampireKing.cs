using UnityEngine;

public class VampireKing : EnemyChase
{
    [Header("Vampire Stats")]
    public float lifeStealRatio = 0.5f;
    public float regenInterval = 1.0f;

    [Header("Phase Settings")]
    public float phase1Speed = 2.0f;
    public float phase2Speed = 5.0f;

    [Header("Phase 1: Summoning")]
    public GameObject minionPrefab;
    public Transform[] summonPoints;
    public float summonInterval = 10f;

    private bool _hitFrame1Success = false;
    private int _frame1DamageDealt = 0;
    private float _regenTimer = 0f;
    private float _lastDamageTime = -1f;
    private const float DAMAGE_EVENT_COOLDOWN = 0.01f;
    private float _lastSummonTime = -999f;

    private bool _hasExplodedOnDeath = false;
    private bool _isTrueForm = false;

    private CapsuleCollider2D _bodyCollider;

    protected override void Start()
    {
        base.Start();

        _bodyCollider = GetComponent<CapsuleCollider2D>();

        // Setup trạng thái ban đầu
        if (currentPhaseIndex == 0)
        {
            _isTrueForm = false;
            chaseSpeed = phase1Speed;
        }
        else
        {
            // Trường hợp load game mà đang ở phase 2
            _isTrueForm = true;
            chaseSpeed = phase2Speed;
        }

        // Cập nhật Animator ngay khi bắt đầu
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

            // Báo cho Animator biết đã sang Phase 2
            UpdateAnimatorPhase();
        }
    }

    // Hàm gửi thông số Phase vào Animator
    private void UpdateAnimatorPhase()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("isTrueForm", _isTrueForm);
        }
    }

    // Logic Skill 3 Frame
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

    private void SummonMinions()
    {
        if (minionPrefab == null || summonPoints == null) return;
        foreach (Transform point in summonPoints)
        {
            if (point != null) Instantiate(minionPrefab, point.position, Quaternion.identity);
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
        base.Dead();
    }
}
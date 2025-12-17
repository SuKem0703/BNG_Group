using UnityEngine;

public class VampireKing : EnemyChase
{
    [Header("Vampire Unique Stats")]
    public float lifeStealRatio = 0.5f;
    public float regenInterval = 1.0f;

    // Biến nội bộ
    private bool _hitFrame1Success = false;
    private float _regenTimer = 0f;
    private float _lastDamageTime = -1f;
    private const float DAMAGE_EVENT_COOLDOWN = 0.1f;
    private bool _hasExplodedOnDeath = false;

    // [MỚI] Tham chiếu Collider để bật tắt xuyên tường
    private CapsuleCollider2D _bodyCollider;

    protected override void Start()
    {
        base.Start(); // Gọi start của cha để khởi tạo các chỉ số cơ bản

        // [MỚI] Lấy Collider của chính object này
        _bodyCollider = GetComponent<CapsuleCollider2D>();

        if (_bodyCollider == null)
        {
            Debug.LogWarning("Vampire King thiếu CapsuleCollider2D!");
        }
    }

    protected override void Update()
    {
        base.Update(); // Chạy logic di chuyển của cha trước

        HandlePassiveRegen();
        HandleGhostMovement(); // [MỚI] Xử lý xuyên tường
    }

    // [MỚI] Hàm xử lý bật tắt Trigger khi truy đuổi
    private void HandleGhostMovement()
    {
        if (_bodyCollider == null || player == null || isDead) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Điều kiện: Đang trong tầm phát hiện VÀ Chưa tới tầm đánh (tức là đang chạy đuổi)
        // Khi đang đánh (đứng yên) thì nên tắt trigger để player không đi xuyên qua người boss
        bool isChasing = distance <= detectionRadius && distance > attackRange;

        // Chỉ set lại khi giá trị thay đổi để tối ưu hiệu năng
        if (_bodyCollider.isTrigger != isChasing)
        {
            _bodyCollider.isTrigger = isChasing;
        }
    }

    // Override lại để vô hiệu hóa logic DealDamage cũ của EnemyChase
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

    public void DealVampireDamage(int frameIndex)
    {
        int _frame1DamageDealt = 0;

        if (Time.time - _lastDamageTime < DAMAGE_EVENT_COOLDOWN) return;
        _lastDamageTime = Time.time;

        if (isDead || isStunned || PauseController.IsGamePause) return;

        if (frameIndex == 1)
        {
            _hitFrame1Success = false;
            _frame1DamageDealt = 0;
        }

        if (player == null ||
            Vector2.Distance(transform.position, player.position) > attackRange)
            return;

        var pStats = player.GetComponentInParent<PlayerStats>();
        if (pStats == null) return;

        switch (frameIndex)
        {
            case 1:
                {
                    int rawDamage = Mathf.RoundToInt(damage * 1.2f);
                    _frame1DamageDealt = pStats.TakeDamage(rawDamage);
                    _hitFrame1Success = _frame1DamageDealt > 0;

                    Debug.Log($"[Frame 1] Raw={rawDamage}, Real={_frame1DamageDealt}");
                    break;
                }

            case 2:
                {
                    if (!_hitFrame1Success || _frame1DamageDealt <= 0) return;

                    int healAmount = Mathf.RoundToInt(_frame1DamageDealt * lifeStealRatio);
                    if (healAmount > 0)
                    {
                        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
                        ShowHealPopup(healAmount);
                    }

                    Debug.Log($"[Frame 2] LifeSteal from real dmg: {healAmount}");
                    break;
                }

            case 3:
                {
                    int rawDamage = Mathf.RoundToInt(damage * 1.5f);
                    int realDamage = pStats.TakeDamage(rawDamage);

                    Debug.Log($"[Frame 3] Raw={rawDamage}, Real={realDamage}");
                    break;
                }
        }
    }


    public void DealDamageWhenDead()
    {
        if (_hasExplodedOnDeath) return;
        _hasExplodedOnDeath = true;

        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRadius)
        {
            var pStats = player.GetComponentInParent<PlayerStats>();
            if (pStats != null)
            {
                int deathDamage = Mathf.RoundToInt(maxHealth * 0.1f);
                pStats.TakeDamage(deathDamage);

                // ShowExplosionPopup(deathDamage); // Bỏ comment nếu muốn hiện popup
                Debug.Log($"[ONE-SHOT] Vampire King explodes dealing {deathDamage} damage!");
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
}
using System;
using System.Collections;
using Unity.Netcode;
using Unity.Networking.Transport.Error;
using UnityEngine;

public class PlayerVitals : NetworkBehaviour
{
    public event Action<int, DamageSourceType> OnDamaged;
    public event Action<int, DamageSourceType> OnHealed;
    public event Action OnDeath;

    [Header("Health (Networked cho người khác thấy)")]
    public NetworkVariable<int> netKnightHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netMageHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netMaxKnightHP = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netMaxMageHP = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> netIsDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Local Resources (Chỉ Owner dùng)")]
    public int knightMP { get; set; } = 50;
    public int mageMP { get; set; } = 50;
    public float currentStamina { get; set; } = 20f;

    [Header("Regen Config")]
    public float healthRegenDelay = 5f;
    public float mpRegenDelay = 2f;
    public float staminaRegenDelay = 1.5f;
    public float healthRegenInterval = 2f;
    public float mpRegenInterval = 1f;
    public float staminaRegenInterval = 0.5f;

    private float healthRegenTimer;
    private float mpRegenTimer;
    private float staminaRegenTimer;

    public bool isInvincible = false;
    public bool isProcessingDeath { get; private set; } = false;
    public bool isGameOver { get; private set; } = false;

    [SerializeField] private PlayerCore core;
    [SerializeField] private Collider2D playerCollider;

    private void Start()
    {
        if (IsOwner)
        {
            core.playerStats.OnStatsUpdated += SyncMaxHPAndClamp;
        }
    }

    private void OnDestroy()
    {
        if (core.playerStats != null) core.playerStats.OnStatsUpdated -= SyncMaxHPAndClamp;
    }

    private void Update()
    {
        if (!IsOwner || PauseController.IsGamePause || isProcessingDeath || isGameOver) return;
        HandleRegen();
    }

    private void SyncMaxHPAndClamp()
    {
        netMaxKnightHP.Value = core.playerStats.finalKnightMaxHP;
        netMaxMageHP.Value = core.playerStats.finalMageMaxHP;

        netKnightHealth.Value = Mathf.Min(netKnightHealth.Value, core.playerStats.finalKnightMaxHP);
        netMageHealth.Value = Mathf.Min(netMageHealth.Value, core.playerStats.finalMageMaxHP);
        knightMP = Mathf.Min(knightMP, core.playerStats.finalKnightMaxMP);
        mageMP = Mathf.Min(mageMP, core.playerStats.finalMageMaxMP);
    }

    public int TakeDamage(int rawDamage)
    {
        if (isInvincible || isProcessingDeath || isGameOver) return 0;

        if (IsServer && !IsOwner)
        {
            TakeDamageClientRpc(rawDamage);
            return rawDamage;
        }

        return ProcessDamageLocally(rawDamage);
    }

    [ClientRpc]
    private void TakeDamageClientRpc(int rawDamage)
    {
        if (IsOwner) ProcessDamageLocally(rawDamage);
    }

    private int ProcessDamageLocally(int rawDamage)
    {
        if (isInvincible || isProcessingDeath || isGameOver) return 0;

        float def = core.playerStats.finalDefense;
        float dmgRed = core.playerStats.damageReduction;

        float mitigation = def / (def + 100f);
        float reductionFactor = (1f - mitigation) * (1f - dmgRed);
        int finalDamage = Mathf.Max(Mathf.CeilToInt(rawDamage * reductionFactor), 1);

        bool isKnight = core.classController.IsKnightActive;
        int currentHP = isKnight ? netKnightHealth.Value : netMageHealth.Value;
        currentHP -= finalDamage;

        OnDamaged?.Invoke(finalDamage, DamageSourceType.Enemy);

        if (isKnight) netKnightHealth.Value = currentHP;
        else netMageHealth.Value = currentHP;

        if (currentHP <= 0)
        {
            isProcessingDeath = true;
            HandleDeath(isKnight ? "Knight" : "Mage");
        }

        return finalDamage;
    }

    public void Heal(int amount, bool isKnight)
    {
        int maxHP = isKnight ? core.playerStats.finalKnightMaxHP : core.playerStats.finalMageMaxHP;
        int currentHP = isKnight ? netKnightHealth.Value : netMageHealth.Value;

        int newHP = Mathf.Min(currentHP + amount, maxHP);
        int actualHeal = newHP - currentHP;

        if (isKnight) netKnightHealth.Value = newHP;
        else netMageHealth.Value = newHP;

        if (actualHeal > 0) OnHealed?.Invoke(actualHeal, DamageSourceType.Heal);
    }

    public void HealActiveCharacter(int amount)
    {
        Heal(amount, core.classController.IsKnightActive);
    }

    public void RecoverMP(int amount, bool isKnight)
    {
        if (isKnight) knightMP = Mathf.Min(knightMP + amount, core.playerStats.finalKnightMaxMP);
        else mageMP = Mathf.Min(mageMP + amount, core.playerStats.finalMageMaxMP);
    }

    public void RecoverMPActiveCharacter(int amount)
    {
        RecoverMP(amount, core.classController.IsKnightActive);
    }

    public void UseMP(int amount, bool isKnight)
    {
        if (isKnight) knightMP = Mathf.Max(knightMP - amount, 0);
        else mageMP = Mathf.Max(mageMP - amount, 0);
        mpRegenTimer = -mpRegenDelay; // Reset cooldown regen
    }

    public void UseStamina(float amount)
    {
        if (amount <= 0) return;
        currentStamina = Mathf.Max(currentStamina - amount, 0);
        staminaRegenTimer = -staminaRegenDelay;
    }

    public bool CanHeal() => (netKnightHealth.Value < core.playerStats.finalKnightMaxHP) || (netMageHealth.Value < core.playerStats.finalMageMaxHP);
    public bool CanRecoverMP() => (knightMP < core.playerStats.finalKnightMaxMP) || (mageMP < core.playerStats.finalMageMaxMP);
    public void SetInvincible(bool value) => isInvincible = value;

    private void HandleRegen()
    {
        bool isKnight = core.classController.IsKnightActive;

        healthRegenTimer += Time.unscaledDeltaTime;
        if (healthRegenTimer >= healthRegenInterval)
        {
            if (isKnight && netKnightHealth.Value < core.playerStats.finalKnightMaxHP)
                netKnightHealth.Value = Mathf.Min(netKnightHealth.Value + core.playerStats.finalHPRegen, core.playerStats.finalKnightMaxHP);
            else if (!isKnight && netMageHealth.Value < core.playerStats.finalMageMaxHP)
                netMageHealth.Value = Mathf.Min(netMageHealth.Value + core.playerStats.finalHPRegen, core.playerStats.finalMageMaxHP);

            healthRegenTimer = 0f;
        }

        // MP Regen
        mpRegenTimer += Time.unscaledDeltaTime;
        if (mpRegenTimer >= mpRegenInterval)
        {
            if (isKnight && knightMP < core.playerStats.finalKnightMaxMP)
                knightMP = Mathf.Min(knightMP + core.playerStats.finalMPRegen, core.playerStats.finalKnightMaxMP);
            else if (!isKnight && mageMP < core.playerStats.finalMageMaxMP)
                mageMP = Mathf.Min(mageMP + core.playerStats.finalMPRegen, core.playerStats.finalMageMaxMP);

            mpRegenTimer = 0f;
        }

        // Stamina Regen
        if (currentStamina < core.playerStats.finalStamina)
        {
            staminaRegenTimer += Time.unscaledDeltaTime;
            if (staminaRegenTimer >= staminaRegenInterval)
            {
                currentStamina = Mathf.Min(currentStamina + core.playerStats.finalStaminaRegen, core.playerStats.finalStamina);
                staminaRegenTimer = 0f;
            }
        }
    }

    private void HandleDeath(string who)
    {
        Debug.Log($"{who} has fallen!");

        OnDeath?.Invoke();
        SetDeathStateServerRpc(true);

        if (playerCollider != null) playerCollider.enabled = false;

        if (core.playerMovement != null)
        {
            core.playerMovement.TriggerDeath();
            if (core.playerMovement.rb != null) core.playerMovement.rb.linearVelocity = Vector2.zero;
        }

        var knightAttack = GetComponentInChildren<KnightNormalAttack>(true);
        if (knightAttack != null) knightAttack.EndAttack();

        var mageAttack = GetComponentInChildren<MageNormalAttack>(true);
        if (mageAttack != null) mageAttack.EndAttack();

        Animator activeAnimator = who == "Knight"
            ? core.classController.knightObject.GetComponentInChildren<Animator>()
            : core.classController.mageObject.GetComponentInChildren<Animator>();

        if (activeAnimator != null)
        {
            activeAnimator.SetBool("isWalking", false);
            activeAnimator.SetBool("isRunning", false);
            activeAnimator.SetTrigger("Die");
        }
    }

    public void OnCharacterDeathAnimationFinished()
    {
        if (!IsOwner) return;
        if (isGameOver) return;

        bool knightAlive = netKnightHealth.Value > 0;
        bool mageAlive = netMageHealth.Value > 0;

        bool isKnightActive = core.classController.IsKnightActive;
        Animator activeAnimator = isKnightActive
            ? core.classController.knightObject.GetComponentInChildren<Animator>()
            : core.classController.mageObject.GetComponentInChildren<Animator>();

        if (isKnightActive)
        {
            if (mageAlive)
            {
                if (activeAnimator != null) { activeAnimator.ResetTrigger("Die"); activeAnimator.Play("Idle"); }
                core.classController.SwitchClass(core.classController.mageObject);
                StartCoroutine(FinalizeRespawnProtection(1.5f));
            }
            else GameOver();
        }
        else
        {
            if (knightAlive)
            {
                if (activeAnimator != null) { activeAnimator.ResetTrigger("Die"); activeAnimator.Play("Idle"); }
                core.classController.SwitchClass(core.classController.knightObject);
                StartCoroutine(FinalizeRespawnProtection(1.5f));
            }
            else GameOver();
        }
    }

    public IEnumerator FinalizeRespawnProtection(float invincibilityDuration = 0.5f)
    {
        SetInvincible(true);
        if (playerCollider != null) playerCollider.enabled = false;
        if (core.playerMovement != null) core.playerMovement.ResetDeathState();

        yield return new WaitForSeconds(invincibilityDuration);

        SetInvincible(false);
        if (playerCollider != null) playerCollider.enabled = true;
        isProcessingDeath = false;
        SetDeathStateServerRpc(false);
    }

    private void GameOver()
    {
        isGameOver = true;
        if (core.playerMovement != null && core.playerMovement.rb != null) core.playerMovement.rb.linearVelocity = Vector2.zero;
        if (PauseController.IsGamePause) return;

        DeathService.Instance.HandlePlayerDeath();
        if (GameOverUIAdapter.Instance != null) GameOverUIAdapter.Instance.ShowGameOverUI();
    }

    [ServerRpc]
    public void SetDeathStateServerRpc(bool isDead)
    {
        netIsDead.Value = isDead;
        if (isDead && core.playerStats != null) core.playerStats.ChangeAggro(-999);
    }

    public void ResetVitals()
    {
        netKnightHealth.Value = core.playerStats.finalKnightMaxHP;
        netMageHealth.Value = core.playerStats.finalMageMaxHP;
        knightMP = core.playerStats.finalKnightMaxMP;
        mageMP = core.playerStats.finalMageMaxMP;
        currentStamina = core.playerStats.finalStamina;

        isProcessingDeath = false;
        isGameOver = false;

        if (playerCollider != null) playerCollider.enabled = true;
        if (core.playerMovement != null) core.playerMovement.ResetDeathState();

        SetDeathStateServerRpc(false);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

[System.Serializable]
public class BossPhaseInfo
{
    [TextArea] public string phaseDescription = "Phase Description";
    public int maxHealth = 1000;
}

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyCombatAI))]
public class Enemy : NetworkBehaviour
{
    [Header("Quest Settings")]
    public string questTargetID;
    public bool isQuestEnemy = false;
    public string UniqueID;

    [Header("Enemy Info")]
    public EnemyRank enemyRank = EnemyRank.Normal;

    [Header("Boss Phases System")]
    public List<BossPhaseInfo> bossPhases = new List<BossPhaseInfo>();
    public int currentPhaseIndex = 0;

    [Header("Enemy Stats")]
    public string enemyName = "Enemy";
    public int levelEnemy = 1;
    public float damage = 10f;
    public int maxHealth = 100;
    public int defense = 0;
    public float experienceReward;
    public float goldReward;

    public NetworkVariable<int> netHealth = new NetworkVariable<int>(100);
    public NetworkVariable<bool> netIsWalking = new NetworkVariable<bool>(false);
    public NetworkVariable<Vector2> netDirection = new NetworkVariable<Vector2>(Vector2.zero);

    [Header("Movement")]
    public float chaseSpeed = 3f;
    public float detectionRadius = 15f;
    public float attackRange = 2f;
    public float attackTriggerBuffer = 1f;
    public float chaseResumeBuffer = -2f;

    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;

    public float lastAttackTime = -999f;
    public bool hasDealtDamageThisAttack = false;

    [Header("Hurt & Knockback Settings")]
    public float hurtDuration = 0.5f;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;

    [Header("Hit Flash Settings")]
    public Material flashMaterial;
    private Material originalMaterial;

    public bool isAttacking = false;
    public bool isStunned = false;
    public bool isDead = false;
    public bool IsDead => isDead;
    public bool isKnockedBack = false;
    public bool isTransitioning = false;
    protected bool hasProcessedDeath = false;

    public Rigidbody2D rb;
    public EnemyAnimator enemyAnimator;
    public SpriteRenderer spriteRenderer;

    [SerializeField] private EnemyHealth healthLogic;
    [SerializeField] private EnemyCombatAI aiLogic;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(questTargetID)) questTargetID = enemyName;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) originalMaterial = spriteRenderer.material;

        if (healthLogic != null) healthLogic.Init(this);

        if (aiLogic != null) aiLogic.Init(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (enemyAnimator != null) enemyAnimator.enabled = true;

        if (IsServer)
        {
            InitializePhase(0);
        }

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
        {
            BossHUD.Instance.ShowBossHealth(this);
        }

        if (isQuestEnemy)
        {
            if (SaveController.IsDataLoaded) CheckPersistence();
            else SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    protected void InitializePhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;
        if (bossPhases != null && bossPhases.Count > phaseIndex)
        {
            maxHealth = bossPhases[phaseIndex].maxHealth;
        }
        netHealth.Value = maxHealth;

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null && IsClient)
        {
            BossHUD.Instance.UpdatePhaseInfo(this);
        }
    }

    public string GetCurrentPhaseName() => enemyName;

    public string GetCurrentPhaseDescription()
    {
        if (bossPhases != null && bossPhases.Count > currentPhaseIndex) return bossPhases[currentPhaseIndex].phaseDescription;
        return "";
    }

    public int GetRemainingPhases()
    {
        if (bossPhases == null) return 0;
        return (bossPhases.Count - 1) - currentPhaseIndex;
    }

    protected virtual void Update()
    {
        if (aiLogic != null) aiLogic.OnUpdate();
    }

    public void OnPlayerDetected(Transform detectedPlayer) => aiLogic?.OnPlayerDetected(detectedPlayer);
    public void OnPlayerLost(Transform lostPlayer) => aiLogic?.OnPlayerLost(lostPlayer);

    public virtual void DealDamage() => aiLogic?.ProcessDealDamage();
    public void EndAttack() => aiLogic?.ProcessEndAttack();

    public void TakeDamage(int rawDamage, DamageSourceType damageSourceType, Transform attacker = null, bool isCritical = false, bool forceKnockback = false)
    {
        healthLogic?.ProcessDamage(rawDamage, damageSourceType, attacker, isCritical, forceKnockback);
    }

    public void HandleHealthDepleted()
    {
        if (bossPhases != null && currentPhaseIndex < bossPhases.Count - 1)
        {
            StartCoroutine(SwitchPhaseRoutine());
        }
        else
        {
            isDead = true;
            Die();
        }
    }

    [ClientRpc]
    public void PerformAttackClientRpc(Vector2 attackDirection)
    {
        if (isDead) return;

        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.SetFacingDirection(attackDirection);
            enemyAnimator.TriggerAttack();
        }
    }

    [ClientRpc]
    public void TakeDamageVisualsClientRpc(int finalDamage, DamageSourceType damageSourceType, bool isCritical)
    {
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.DamagePopupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(LoadResourceManager.Instance.DamagePopupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(finalDamage, damageSourceType, isCritical);
        }

        if (isCritical && CinemachineShaker.Instance != null)
        {
            CinemachineShaker.Instance.TriggerShake(2f, 2f, 0.2f);
        }

        if (!isDead && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashSpriteRoutine());
        }
    }

    private IEnumerator FlashSpriteRoutine()
    {
        if (spriteRenderer != null)
        {
            if (flashMaterial != null)
            {
                spriteRenderer.material = flashMaterial;
                yield return new WaitForSeconds(0.08f);
                spriteRenderer.material = originalMaterial;
            }
            else
            {
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.2f);
                yield return new WaitForSeconds(0.08f);
                spriteRenderer.color = originalColor;
            }
        }
    }

    protected IEnumerator SwitchPhaseRoutine()
    {
        isTransitioning = true;
        isStunned = true;
        aiLogic?.StopMovement();

        SwitchPhaseVisualsClientRpc();

        yield return new WaitForSeconds(2.0f);

        int nextPhase = currentPhaseIndex + 1;
        InitializePhase(nextPhase);
        OnPhaseChange(nextPhase);

        isTransitioning = false;
        isStunned = false;
    }

    [ClientRpc]
    private void SwitchPhaseVisualsClientRpc()
    {
        if (isDead) return;
        if (enemyAnimator != null) enemyAnimator.TriggerDie();
    }

    protected virtual void OnPhaseChange(int nextPhaseIndex)
    {
        if (enemyAnimator != null)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.Play("Idle");
            enemyAnimator.SetWalking(false);
        }
    }

    [ClientRpc]
    public void TriggerHurtClientRpc()
    {
        if (isDead) return;
        if (enemyAnimator != null) enemyAnimator.TriggerHurt();
    }

    public virtual void Die()
    {
        if (hasProcessedDeath) return;
        hasProcessedDeath = true;

        isStunned = false;
        isAttacking = false;
        isKnockedBack = false;

        PlayerStats.IsOnBattle = false;

        healthLogic?.StopHurt();
        aiLogic?.StopMovement();

        int expGain = Mathf.FloorToInt(experienceReward * Random.Range(0.9f, 1.1f));
        int goldGain = Mathf.FloorToInt(goldReward * Random.Range(0.9f, 1.1f));

        if (PlayerStats.Instance != null && expGain > 0)
            PlayerStats.Instance.AddEXP(expGain);

        if (EconomyService.Instance != null && goldGain > 0)
        {
            EconomyService.Instance.EarnCurrency("Coin", goldGain, $"Kill: {enemyName}", (success) => {
                if (success && PlayerStats.Instance != null)
                    PlayerStats.Instance.SyncCoinFromServer(PlayerStats.Instance.coin + goldGain);
            });
        }

        if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID))
            QuestController.Instance.MarkEnemyDefeated(questTargetID);

        if (isQuestEnemy && SaveController.Instance != null && !string.IsNullOrEmpty(UniqueID))
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, UniqueID);
            SaveController.Instance.TriggerAutoSave();
        }

        DieVisualsClientRpc();
    }

    [ClientRpc]
    private void DieVisualsClientRpc()
    {
        isDead = true;

        if (enemyRank == EnemyRank.Boss && BossHUD.Instance != null)
            BossHUD.Instance.HideBossHealth();

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Hurt");
            anim.SetBool("IsAttacking", false);

            anim.SetTrigger("isDie");
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.SetWalking(false);
            enemyAnimator.TriggerDie();
        }
    }

    protected virtual void Dead()
    {
        StopAllCoroutines();

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
        }

        gameObject.SetActive(false);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (isQuestEnemy) SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        CheckPersistence();
    }

    private void CheckPersistence()
    {
        if (SaveController.Instance != null && !string.IsNullOrEmpty(UniqueID))
        {
            if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, UniqueID))
            {
                if (NetworkObject != null && NetworkObject.IsSpawned && IsServer)
                {
                    NetworkObject.Despawn(false);
                }
                gameObject.SetActive(false);
            }
        }
    }
}
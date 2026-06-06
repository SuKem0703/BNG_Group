using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
public class Enemy : NetworkBehaviour, ITargetableInfo
{
    [Header("Data Core")]
    public EnemyData data;

    [Header("Quest & Instance Settings")]
    public bool isQuestEnemy = false;
    public string UniqueID;

    public EnemySpawnArea parentArea { get; private set; }

    public string enemyName => data != null ? data.enemyName : "Enemy";
    public string questTargetID => data != null ? (string.IsNullOrEmpty(data.questTargetID) ? data.enemyName : data.questTargetID) : "";
    public EnemyRank enemyRank => data != null ? data.enemyRank : EnemyRank.Normal;
    public EnemyRace enemyRace => data != null ? data.enemyRace : EnemyRace.Slime;
    public int levelEnemy => data != null ? data.levelEnemy : 1;

    public IReadOnlyList<BossPhaseInfo> bossPhases => data != null ? data.bossPhases : null;

    public float experienceReward { get; private set; }
    public float goldReward { get; private set; }
    public float damage { get; set; }
    public int maxHealth { get; set; }
    public int defense { get; set; }
    public float chaseSpeed { get; set; }
    public float detectionRadius { get; set; }

    public float attackRange { get; set; }
    public float attackCooldown { get; set; }

    [Header("Runtime State")]
    public int currentPhaseIndex = 0;
    public NetworkVariable<int> netHealth = new NetworkVariable<int>(100);
    public NetworkVariable<bool> netIsWalking = new NetworkVariable<bool>(false);
    public NetworkVariable<Vector2> netDirection = new NetworkVariable<Vector2>(Vector2.zero);

    [Header("Combat Tracking")]
    private Dictionary<ulong, int> damageContributions = new Dictionary<ulong, int>();

    [Header("Movement & Combat Buffers")]
    public float attackTriggerBuffer = 1f;
    public float chaseResumeBuffer = -2f;
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
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalMaterial = spriteRenderer.material;
        if (healthLogic != null) healthLogic.Init(this);
        if (aiLogic != null) aiLogic.Init(this);

        ApplyTimeScalingStats();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (enemyAnimator != null) enemyAnimator.enabled = true;

        if (IsServer)
        {
            InitializePhase(0);
        }

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = IsServer; 
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

    public void SetSpawnArea(EnemySpawnArea area)
    {
        parentArea = area;
    }

    private void GetTimeMultipliers(out float statMult, out float speedMult, out float rewardMult)
    {
        statMult = 1.0f;
        speedMult = 1.0f;
        rewardMult = 1.0f;

        if (data == null || TimeManager.Instance == null || data.timeScalingType == TimeScalingType.None) return;

        if (data.timeScalingType == TimeScalingType.Nocturnal)
        {
            switch (TimeManager.CurrentPeriod)
            {
                case TimePeriod.Evening:
                    statMult = 1.2f; speedMult = 1.1f; rewardMult = 1.25f; break;
                case TimePeriod.Night:
                    statMult = 1.5f; speedMult = 1.3f; rewardMult = 1.5f; break;
            }
        }
        else if (data.timeScalingType == TimeScalingType.Diurnal)
        {
            switch (TimeManager.CurrentPeriod)
            {
                case TimePeriod.Morning:
                case TimePeriod.Afternoon:
                    statMult = 1.5f; speedMult = 1.3f; rewardMult = 1.5f; break;
                case TimePeriod.Evening:
                    statMult = 1.0f; speedMult = 1.0f; rewardMult = 1.0f; break;
                case TimePeriod.Night:
                    statMult = 0.7f; speedMult = 0.8f; rewardMult = 0.8f; break;
            }
        }
    }

    private void ApplyTimeScalingStats()
    {
        if (data == null) return;

        GetTimeMultipliers(out float statMult, out float speedMult, out float rewardMult);

        damage = data.damage * statMult;
        defense = Mathf.RoundToInt(data.defense * statMult);
        chaseSpeed = data.chaseSpeed * speedMult;
        detectionRadius = data.detectionRadius * speedMult;

        experienceReward = data.experienceReward * rewardMult;
        goldReward = data.goldReward * rewardMult;

        attackRange = data.attackRange;
        attackCooldown = data.attackCooldown;

        if (bossPhases == null || bossPhases.Count == 0)
        {
            maxHealth = Mathf.RoundToInt(data.maxHealth * statMult);
        }
    }

    protected void InitializePhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;

        GetTimeMultipliers(out float statMult, out _, out _);

        if (bossPhases != null && bossPhases.Count > phaseIndex)
        {
            maxHealth = Mathf.RoundToInt(bossPhases[phaseIndex].maxHealth * statMult);
        }
        else if (data != null)
        {
            maxHealth = Mathf.RoundToInt(data.maxHealth * statMult);
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
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (PauseController.IsGamePause)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            if (isAttacking)
            {
                isAttacking = false;
                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("IsAttacking", false);
                }
            }
            return;
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (agent.isStopped && !isAttacking && !isStunned && !isKnockedBack && !isDead)
            {
                agent.isStopped = false;
            }
        }

        if (aiLogic != null) aiLogic.OnUpdate();
    }

    public void OnPlayerDetected(Transform detectedPlayer)
    {
        aiLogic?.OnPlayerDetected(detectedPlayer);

        if (data != null && SaveController.Instance != null)
        {
            SaveController.Instance.UnlockBestiary(data.enemyName, 1);
        }
    }

    public void OnPlayerLost(Transform lostPlayer) => aiLogic?.OnPlayerLost(lostPlayer);

    public virtual void DealDamage() => aiLogic?.ProcessDealDamage();
    public void EnemyEndAttack() => aiLogic?.ProcessEndAttack();

    public void TakeDamage(int rawDamage, DamageSourceType damageSourceType, Transform attacker = null, bool isCritical = false, bool forceKnockback = false)
    {
        if (IsServer && attacker != null)
        {
            NetworkObject attackerNetObj = attacker.GetComponent<NetworkObject>();
            if (attackerNetObj == null) attackerNetObj = attacker.GetComponentInParent<NetworkObject>();

            if (attackerNetObj != null)
            {
                ulong clientId = attackerNetObj.OwnerClientId;

                float def = defense;
                float mitigation = def / (def + 100f);
                int finalDamage = Mathf.Max(Mathf.CeilToInt(rawDamage * (1f - mitigation)), 1);

                if (damageContributions.ContainsKey(clientId))
                {
                    damageContributions[clientId] += finalDamage;
                }
                else
                {
                    damageContributions.Add(clientId, finalDamage);
                }
            }
        }

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
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.SetBool("IsChasing", false);

            enemyAnimator.SetFacingDirection(attackDirection);
            
            enemyAnimator.TriggerAttack();
        }
    }

    [ClientRpc]
    public void TakeDamageVisualsClientRpc(int finalDamage, DamageSourceType damageSourceType, bool isCritical)
    {
        if (DamagePopupPool.Instance != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            DamagePopup popup = DamagePopupPool.Instance.GetPopup(spawnPosition);
            popup.Setup(finalDamage, damageSourceType, isCritical);
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
            enemyAnimator.EndAttack();
        }
    }

    [ClientRpc]
    public void TriggerHurtClientRpc()
    {
        if (isDead) return;
        if (enemyAnimator != null) enemyAnimator.TriggerHurt();
    }

    public virtual void Die(bool giveReward = true)
    {
        if (hasProcessedDeath) return;
        hasProcessedDeath = true;

        isStunned = false;
        isAttacking = false;
        isKnockedBack = false;

        healthLogic?.StopHurt();
        aiLogic?.StopMovement();

        if (giveReward)
        {
            if (data != null && SaveController.Instance != null)
            {
                SaveController.Instance.RecordEnemyDefeat(data.enemyName);
            }

            uint subSeed = SaveController.MasterSeed + (uint)NetworkObjectId;
            SeededRandom rng = new SeededRandom(subSeed);

            int totalExpDrop = Mathf.FloorToInt(experienceReward * (0.9f + rng.NextFloat() * 0.2f));
            int totalGoldDrop = Mathf.FloorToInt(goldReward * (0.9f + rng.NextFloat() * 0.2f));
            ulong mvpClientId = 999;

            if (IsServer)
            {
                if (damageContributions.Count > 0)
                {
                    var mvpEntry = damageContributions.OrderByDescending(x => x.Value).First();
                    mvpClientId = mvpEntry.Key;
                    float totalDamageGained = damageContributions.Sum(x => x.Value);

                    foreach (var entry in damageContributions)
                    {
                        ulong clientId = entry.Key;
                        int dmgDone = entry.Value;

                        float contributionRatio = (float)dmgDone / totalDamageGained;

                        int playerExp = Mathf.FloorToInt(totalExpDrop * contributionRatio);
                        int playerGold = Mathf.FloorToInt(totalGoldDrop * contributionRatio);

                        if (playerExp > 0 || playerGold > 0)
                        {
                            ClientRpcParams clientRpcParams = new ClientRpcParams
                            {
                                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                            };
                            RewardKillerClientRpc(playerExp, playerGold, clientRpcParams);
                        }
                    }
                }
            }

            if (QuestController.Instance != null && !string.IsNullOrEmpty(questTargetID))
                QuestController.Instance.MarkEnemyDefeated(questTargetID);

            if (isQuestEnemy && SaveController.Instance != null && !string.IsNullOrEmpty(UniqueID))
            {
                SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, UniqueID);
                SaveController.Instance.TriggerAutoSave();
            }

            if (IsServer && data != null && data.lootTable != null)
            {
                int droppedItemID = data.lootTable.GetRandomDrop(rng);

                if (droppedItemID > 0)
                {
                    GameObject prefabToDrop = ItemDictionary.Instance != null ? ItemDictionary.Instance.GetItemPrefab(droppedItemID) : null;

                    if (prefabToDrop != null)
                    {
                        Vector3 dropPos = transform.position + new Vector3(0, 0.5f, 0);
                        GameObject droppedItem = Instantiate(prefabToDrop, dropPos, Quaternion.identity);

                        Item item = droppedItem.GetComponent<Item>();
                        if (item != null)
                        {
                            item.ownerClientId = mvpClientId;

                            uint itemStatSeed = (uint)rng.NextInt(0, int.MaxValue);
                            SeededRandom itemRng = new SeededRandom(itemStatSeed);

                            item.rarity = ItemGenerationHelper.GetRandomRarity(itemRng);
                            item.qualityFactor = ItemGenerationHelper.GetWeightedQualityFactor(itemRng);
                            item.dropSeed = itemStatSeed;
                        }

                        var netObj = droppedItem.GetComponent<NetworkObject>();
                        if (netObj != null && !netObj.IsSpawned) netObj.Spawn();

                        BounceEffect bounce = droppedItem.GetComponent<BounceEffect>();
                        if (bounce != null)
                        {
                            bounce.StartBounce();
                        }
                    }
                }
            }
        }

        DieVisualsClientRpc();
    }

    [ClientRpc]
    private void RewardKillerClientRpc(int expGain, int goldGain, ClientRpcParams clientRpcParams = default)
    {
        if (PlayerStats.Instance != null && expGain > 0)
            PlayerStats.Instance.AddEXP(expGain);

        if (EconomyService.Instance != null && goldGain > 0)
        {
            EconomyService.Instance.EarnCurrency("Coin", goldGain, $"Kill: {enemyName}", (success) => {
                if (success && PlayerStats.Instance != null)
                    PlayerWallet.Instance.SyncCoinFromServer(PlayerWallet.Instance.coin + goldGain);
            });
        }

        if (goldGain > 0 && GoldEffectPool.Instance != null)
        {
            GoldEffectPool.Instance.SpawnGold(transform.position, goldGain);
        }
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
            anim.ResetTrigger("Hurt");
            anim.SetBool("IsAttacking", false);

            anim.SetTrigger("isDie");
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.EndAttack();
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

    public virtual void ResetEnemyState()
    {
        ApplyTimeScalingStats();

        isDead = false;
        hasProcessedDeath = false;
        isStunned = false;
        isAttacking = false;
        isKnockedBack = false;
        isTransitioning = false;
        damageContributions.Clear();

        if (enemyAnimator != null)
        {
            enemyAnimator.EndAttack();
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (IsServer)
        {
            InitializePhase(0);
        }
    }

    public TargetInfoData GetInfo()
    {
        string raceDisplayName = GetRaceDisplayName(enemyRace);

        string infoText = $"Lv. {levelEnemy} - {raceDisplayName}";

        return new TargetInfoData(
            enemyName,
            data != null ? data.enemyIcon : null,
            infoText,
            TargetType.Enemy
        );
    }

    private string GetRaceDisplayName(EnemyRace race)
    {
        switch (race)
        {
            case EnemyRace.Slime: return "Slime";
            case EnemyRace.Orc: return "Orc";
            case EnemyRace.Goblin: return "Goblin";
            case EnemyRace.Golem: return "Golem";
            case EnemyRace.PredatorPlant: return "Thực vật ăn thịt";
            case EnemyRace.Beholder: return "Beholder";
            case EnemyRace.Imp: return "Tiểu quỷ";
            case EnemyRace.Ghost: return "Hồn ma";
            case EnemyRace.Zombie: return "Thây ma";
            case EnemyRace.Demon: return "Ác quỷ";
            case EnemyRace.Lizardman: return "Người thằn lằn";
            case EnemyRace.GiantRat: return "Chuột khổng lồ";
            case EnemyRace.Vampire: return "Ma cà rồng";
            case EnemyRace.Mushroom: return "Nấm đột biến";
            case EnemyRace.Ent: return "Mộc tinh";
            case EnemyRace.Lich: return "Lich";
            case EnemyRace.Skeleton: return "Bộ xương";
            case EnemyRace.Gnoll: return "Gnoll";
            default: return "Quái vật";
        }
    }
}
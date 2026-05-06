using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Info")]
    public string enemyName = "Enemy";
    public string questTargetID;
    public EnemyRank enemyRank = EnemyRank.Normal;

    [Header("Base Stats")]
    public int levelEnemy = 1;
    public float damage = 10f;
    public int maxHealth = 100;
    public int defense = 0;
    public float experienceReward = 10f;
    public float goldReward = 5f;

    [Header("Movement & Combat")]
    public float chaseSpeed = 3f;
    public float detectionRadius = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;

    [Header("Lore & Description")]
    public string enemyDescription = "Nhập mô tả về kẻ thù tại đây...";

    [Header("Boss Phases (Optional)")]
    public List<BossPhaseInfo> bossPhases;
}
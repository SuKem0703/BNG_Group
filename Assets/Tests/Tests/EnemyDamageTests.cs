using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

public class EnemyDamageTests
{
    private GameObject enemyObject;
    private Enemy enemy;

    [SetUp]
    public void Setup()
    {
        enemyObject = new GameObject();

        Rigidbody2D rigidbody = enemyObject.AddComponent<Rigidbody2D>();
        enemy = enemyObject.AddComponent<Enemy>();

        FieldInfo rbField = typeof(Enemy).GetField("rb", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rbField != null)
        {
            rbField.SetValue(enemy, rigidbody);
        }

        enemy.maxHealth = 100;
        enemy.currentHealth = 100;
        enemy.defense = 0;
        enemy.enemyRank = EnemyRank.Normal;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void WB_DMG_01_TakeDamage_NormalHit_CalculatesCorrectMitigation()
    {
        enemy.defense = 100;

        enemy.TakeDamage(50, DamageSourceType.Enemy);

        Assert.AreEqual(75, enemy.currentHealth);
    }

    [Test]
    public void WB_DMG_02_TakeDamage_HighDefense_ReturnsMinimumOneDamage()
    {
        enemy.defense = 900;

        enemy.TakeDamage(5, DamageSourceType.Enemy);

        Assert.AreEqual(99, enemy.currentHealth);
    }

    [Test]
    public void WB_DMG_03_TakeDamage_LethalDamage_TriggersDeath()
    {
        enemy.defense = 0;

        enemy.TakeDamage(150, DamageSourceType.Enemy);

        Assert.IsTrue(enemy.IsDefeated());
    }

    [Test]
    public void WB_DMG_04_TakeDamage_AlreadyDead_IgnoresDamage()
    {
        enemy.TakeDamage(100, DamageSourceType.Enemy);
        Assert.IsTrue(enemy.IsDefeated());

        enemy.TakeDamage(50, DamageSourceType.Enemy);

        Assert.AreEqual(0, enemy.currentHealth);
    }

    [Test]
    public void WB_DMG_05_TakeDamage_BossPhaseTransition()
    {
        enemy.enemyRank = EnemyRank.Boss;
        enemy.currentHealth = 50;

        enemy.TakeDamage(100, DamageSourceType.Enemy);

        Assert.AreEqual(0, enemy.currentHealth);
        Assert.IsFalse(enemy.IsDead);
    }
}
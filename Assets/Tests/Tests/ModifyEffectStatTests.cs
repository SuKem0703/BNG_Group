using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

public class ModifyEffectStatTests
{
    private GameObject playerObj;
    private PlayerStats stats;

    [SetUp]
    public void Setup()
    {
        playerObj = new GameObject();
        stats = playerObj.AddComponent<PlayerStats>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(playerObj);
    }

    [Test]
    public void WB_STAT_01_ModifySTR_IncreasesCorrectValue()
    {
        stats.ModifyEffectStat("STR", 5);
        var field = typeof(PlayerStats).GetField("effectSTR", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.AreEqual(5, (int)field.GetValue(stats));
    }

    [Test]
    public void WB_STAT_02_ModifyDEX_IncreasesCorrectValue()
    {
        stats.ModifyEffectStat("DEX", 10);
        var field = typeof(PlayerStats).GetField("effectDEX", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.AreEqual(10, (int)field.GetValue(stats));
    }

    [Test]
    public void WB_STAT_04_ModifyCON_IncreasesCorrectValue()
    {
        stats.ModifyEffectStat("CON", 12);
        var field = typeof(PlayerStats).GetField("effectCON", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.AreEqual(12, (int)field.GetValue(stats));
    }

    [Test]
    public void WB_STAT_03_ModifyINT_IncreasesCorrectValue()
    {
        stats.ModifyEffectStat("INT", 7);
        var field = typeof(PlayerStats).GetField("effectINT", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.AreEqual(7, (int)field.GetValue(stats));
    }
}
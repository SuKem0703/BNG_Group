using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

public class PlayerExpTieredTests
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

    private void SetLevel(int level)
    {
        var field = typeof(PlayerStats).GetField("<level>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(stats, level);
    }

    [Test]
    public void WB_EXP_01_Tier1_Level1_Calculation()
    {
        SetLevel(1);
        Assert.AreEqual(151, stats.expToNextLevel);
    }

    [Test]
    public void WB_EXP_04_Tier2_Level100_SwitchFormula()
    {
        SetLevel(100);
        Assert.AreEqual(108100, stats.expToNextLevel);
    }

    [Test]
    public void WB_EXP_03_Tier1_Level99_Boundary()
    {
        SetLevel(99);
        Assert.AreEqual(29619, stats.expToNextLevel);
    }

    [Test]
    public void WB_EXP_06_Tier2_Level199_Boundary()
    {
        SetLevel(199);
        Assert.AreEqual(574660, stats.expToNextLevel);
    }

    [Test]
    public void WB_EXP_07_Tier3_Level200_SwitchFormula()
    {
        SetLevel(200);
        Assert.AreEqual(8020100, stats.expToNextLevel);
    }

    [Test]
    public void WB_EXP_09_Level0_MinimumValue()
    {
        SetLevel(0);
        Assert.AreEqual(100, stats.expToNextLevel);
    }

    [Test]
    public void WB_EXP_10_Level500_LargeValue()
    {
        SetLevel(500);
        Assert.AreEqual(125050100, stats.expToNextLevel);
    }
}
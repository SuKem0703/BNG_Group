using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class SaveControllerTests
{
    private GameObject saveObj;
    private SaveController saveCtrl;

    [SetUp]
    public void Setup()
    {
        saveObj = new GameObject();
        saveCtrl = saveObj.AddComponent<SaveController>();
        saveCtrl.collectedByScene = new List<SaveController.SceneCollected>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(saveObj);
    }

    [Test]
    public void WB_SAVE_01_MarkCollected_NewScene_AddsSceneAndID()
    {
        saveCtrl.MarkCollected("Map01", "Chest_A");
        Assert.AreEqual(1, saveCtrl.collectedByScene.Count);
        Assert.IsTrue(saveCtrl.collectedByScene[0].collectedIDs.Contains("Chest_A"));
    }

    [Test]
    public void WB_SAVE_02_MarkCollected_ExistingSceneNewID_AddsIDOnly()
    {
        saveCtrl.MarkCollected("Map01", "Chest_A");
        saveCtrl.MarkCollected("Map01", "Chest_B");

        Assert.AreEqual(1, saveCtrl.collectedByScene.Count);
        Assert.AreEqual(2, saveCtrl.collectedByScene[0].collectedIDs.Count);
    }

    [Test]
    public void WB_SAVE_03_MarkCollected_ExistingSceneExistingID_IgnoresDuplicate()
    {
        saveCtrl.MarkCollected("Map01", "Chest_A");
        saveCtrl.MarkCollected("Map01", "Chest_A");

        Assert.AreEqual(1, saveCtrl.collectedByScene[0].collectedIDs.Count);
    }
}
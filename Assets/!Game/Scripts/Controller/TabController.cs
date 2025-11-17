using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;

    [Header("Tab Flag Checker")]
    private const int TAB_INDEX_EQUIPMENT = 3;
    private const int TAB_INDEX_POTENTIAL = 4;
    private const int TAB_INDEX_SKILL = 5;

    [Header("Âm thanh")]
    [SerializeField] private AudioClip tabClickSoundClip;

    //void Awake()
    //{
    //    SaveController.OnDataLoaded += HandleDataLoaded;
    //}
    //void OnDestroy()
    //{
    //    SaveController.OnDataLoaded -= HandleDataLoaded;
    //}
    //void OnEnable()
    //{
    //    if (SaveController.IsDataLoaded)
    //    {
    //        // Delay one frame so other components can finish their Start/OnEnable
    //        StartCoroutine(DelayedInit());
    //    }
    //}

    //private IEnumerator DelayedInit()
    //{
    //    yield return null;
    //    UpdateTabVisibility();
    //    ActivateTab(0);
    //}

    //private void HandleDataLoaded()
    //{
    //    if (gameObject.activeInHierarchy)
    //        StartCoroutine(DelayedInit());
    //}

    private void OnEnable()
    {
        SaveController.OnDataLoaded += HandleDataLoaded;
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        UpdateTabVisibility();
        ActivateTab(0);
    }

    private void UpdateTabVisibility()
    {
        if (tabImages == null || tabImages.Length == 0) return;

        for (int i = 0; i < tabImages.Length; i++)
        {
            bool shouldBeVisible = true;

            switch (i)
            {
                case TAB_INDEX_EQUIPMENT:
                    shouldBeVisible = GameFlags.IsOpenedEquipmentMenu();
                    break;
                case TAB_INDEX_POTENTIAL:
                    shouldBeVisible = GameFlags.IsOpenedPotentialMenu();
                    break;
                case TAB_INDEX_SKILL:
                    shouldBeVisible = GameFlags.IsOpenedSkillMenu();
                    break;
            }

            if (tabImages[i] != null)
            {
                tabImages[i].gameObject.SetActive(shouldBeVisible);
            }
        }
    }

    public void ActivateTab(int tabNo)
    {
        if (tabImages == null || tabImages.Length == 0 || pages == null || pages.Length == 0) return;

        // Find a valid tab to activate: prefer requested if visible, otherwise first visible one
        int validTab = -1;
        if (tabNo >= 0 && tabNo < tabImages.Length && tabImages[tabNo] != null && tabImages[tabNo].gameObject.activeSelf)
        {
            validTab = tabNo;
        }
        else
        {
            for (int i = 0; i < tabImages.Length; i++)
            {
                if (tabImages[i] != null && tabImages[i].gameObject.activeSelf)
                {
                    validTab = i;
                    break;
                }
            }
        }

        if (validTab == -1) return; // no visible tab

        int pageCount = pages.Length;
        int tabCount = tabImages.Length;

        for (int i = 0; i < pageCount; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(false);
            if (i < tabCount && tabImages[i] != null)
                tabImages[i].color = Color.grey;
        }

        if (validTab < pageCount && pages[validTab] != null)
            pages[validTab].SetActive(true);
        if (validTab < tabCount && tabImages[validTab] != null)
            tabImages[validTab].color = Color.white;
    }
    public void PointerDown()
    {
        if (tabClickSoundClip != null)
        {
            SoundEffectManager.PlayVoice(tabClickSoundClip);
        }
    }
}

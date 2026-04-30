using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabController : MonoBehaviour
{
    public static TabController Instance;

    public Image[] tabImages;
    public GameObject[] pages;

    [Header("Tab Localization Keys")]
    [SerializeField] private string[] tabKeys;

    [Header("Tab Flag Checker")]
    //private const int TAB_INDEX_EQUIPMENT = 3;
    //private const int TAB_INDEX_POTENTIAL = 4;
    //private const int TAB_INDEX_SKILL = 5;

    [Header("Âm thanh")]
    [SerializeField] private AudioClip tabClickSoundClip;

    private int currentTabIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        SaveController.OnDataLoaded += HandleDataLoaded;
        LocalizationManager.OnLanguageChanged += UpdateTabTexts;

        if (SaveController.IsDataLoaded)
        {
            HandleDataLoaded();
        }

        UpdateTabTexts();
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        LocalizationManager.OnLanguageChanged -= UpdateTabTexts;
    }

    private void HandleDataLoaded()
    {
        UpdateTabVisibility();
        ActivateTab(currentTabIndex);
    }

    // --- HÀM CẬP NHẬT NGÔN NGỮ TAB ---
    private void UpdateTabTexts()
    {
        if (tabImages == null || tabKeys == null) return;
        if (LocalizationManager.Instance == null) return;

        for (int i = 0; i < tabImages.Length; i++)
        {
            if (i >= tabKeys.Length) break;
            if (tabImages[i] == null) continue;

            TextMeshProUGUI tabText = tabImages[i].GetComponentInChildren<TextMeshProUGUI>();

            if (tabText != null && !string.IsNullOrEmpty(tabKeys[i]))
            {
                tabText.text = LocalizationManager.Instance.GetText(tabKeys[i]);
            }
        }
    }

    private void UpdateTabVisibility()
    {
        if (tabImages == null || tabImages.Length == 0) return;

        for (int i = 0; i < tabImages.Length; i++)
        {
            bool shouldBeVisible = true;

            //switch (i)
            //{
            //    case TAB_INDEX_EQUIPMENT:
            //        shouldBeVisible = GameFlags.IsOpenedEquipmentMenu();
            //        break;
            //    case TAB_INDEX_POTENTIAL:
            //        shouldBeVisible = GameFlags.IsOpenedPotentialMenu();
            //        break;
            //    case TAB_INDEX_SKILL:
            //        shouldBeVisible = GameFlags.IsOpenedSkillMenu();
            //        break;
            //}

            if (tabImages[i] != null)
            {
                tabImages[i].gameObject.SetActive(shouldBeVisible);
            }
        }
    }

    public void ActivateTab(int tabNo)
    {
        if (tabImages == null || tabImages.Length == 0 || pages == null || pages.Length == 0) return;

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

        if (validTab == -1) return;

        currentTabIndex = validTab;

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
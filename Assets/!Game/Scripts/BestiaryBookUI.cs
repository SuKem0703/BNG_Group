using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BestiaryBookUI : MonoBehaviour
{
    [Header("Book Flow")]
    public Button openIconButton;

    public GameObject closeButton;

    public GameObject bookAnimatorObject;
    public Animator bookAnimator;
    public GameObject informationPanel;

    [Header("Bestiary Grid")]
    public Transform gridContent;
    public GameObject bestiarySlotPrefab;

    private bool _hasPopulatedGrid = false;
    private bool _isBookOpen = false;

    private void Start()
    {
        if (openIconButton != null) openIconButton.onClick.AddListener(StartOpeningBook);

        bookAnimatorObject.SetActive(false);
        informationPanel.SetActive(false);
        closeButton.SetActive(false);
    }

    private void Update()
    {
        if (_isBookOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            StartClosingBook();
        }
    }

    private void StartOpeningBook()
    {
        openIconButton.gameObject.SetActive(false);
        bookAnimatorObject.SetActive(true);
        closeButton.SetActive(true);

        bookAnimator.SetTrigger("Open");
    }

    public void OnBookFullyOpened()
    {
        informationPanel.SetActive(true);
        _isBookOpen = true;
        PopulateBestiaryGrid();
    }

    public void StartClosingBook()
    {
        if (!_isBookOpen) return;
        _isBookOpen = false;

        informationPanel.SetActive(false);

        if (bookAnimator != null)
        {
            bookAnimator.SetTrigger("Close");
        }
    }

    public void OnBookFullyClosed()
    {
        bookAnimatorObject.SetActive(false);
        closeButton.SetActive(false);

        if (openIconButton != null)
        {
            openIconButton.gameObject.SetActive(true);
        }
    }

    private void PopulateBestiaryGrid()
    {
        if (_hasPopulatedGrid) return;

        if (EnemyDictionary.Instance == null || SaveController.Instance == null)
        {
            Debug.LogWarning("Chưa load đủ hệ thống từ điển hoặc save.");
            return;
        }

        foreach (Transform child in gridContent)
        {
            Destroy(child.gameObject);
        }

        var bestiaryCache = SaveController.Instance.GetBestiaryCache();

        foreach (EnemyData enemyData in EnemyDictionary.Instance.enemyDatabase)
        {
            if (enemyData == null) continue;

            int status = 0;
            if (bestiaryCache.TryGetValue(enemyData.enemyName, out var entry))
            {
                status = entry.status;
            }

            GameObject slotGO = Instantiate(bestiarySlotPrefab, gridContent);
            BestiarySlot slotUI = slotGO.GetComponent<BestiarySlot>();

            if (slotUI != null)
            {
                slotUI.Setup(enemyData, status);
            }
        }

        _hasPopulatedGrid = true;
    }
}
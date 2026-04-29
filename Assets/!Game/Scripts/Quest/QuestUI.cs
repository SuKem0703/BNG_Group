using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;

    public Transform questListContent;
    public GameObject questEntryPrefab;
    public GameObject objectTextPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (questListContent == null)
        {
            questListContent = GameObject.Find("GameUI/Static_UI/Menu/Pages/QuestPage/QuestScroll/Viewport/Content").transform;
        }
    }
    void Start()
    {
        UpdateQuestUI();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void UpdateQuestUI()
    {
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var quest in QuestController.Instance.activeQuests)
        {
            GameObject questEntry = Instantiate(questEntryPrefab, questListContent);
            TMP_Text questNameText = questEntry.transform.Find("QuestNameText").GetComponent<TMP_Text>();
            Transform objectList = questEntry.transform.Find("ObjectList");

            questNameText.text = quest.quest.questName;

            foreach (var questObject in quest.questObjects)
            {
                GameObject objectText = Instantiate(objectTextPrefab, objectList);
                TMP_Text objectTextComponent = objectText.GetComponent<TMP_Text>();
                objectTextComponent.text = $"{questObject.objectTitle} ({questObject.currentAmount}/{questObject.requiredAmount})";
            }
        }
    }
}

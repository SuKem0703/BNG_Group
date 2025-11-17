using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static DialogueController instance { get; private set; }

    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

    public Transform choiceContainer;

    public Image continueIndicator;

    public GameObject choiceButtonPrefab;
    void Awake()
    {
        dialoguePanel = GameObject.Find("DialoguePanel");
        dialogueText = dialoguePanel.transform.Find("DialogueText").GetComponent<TMP_Text>();
        nameText = dialoguePanel.transform.Find("NPCNameText").GetComponent<TMP_Text>();
        portraitImage = dialoguePanel.transform.Find("DialoguePortrait").GetComponent<Image>();
        choiceContainer = dialoguePanel.transform.Find("ChoiceContainer");

        continueIndicator = dialoguePanel.transform.Find("ContinueIndicator").GetComponent<Image>();
        continueIndicator.gameObject.SetActive(false);

        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }
    private void Start()
    {
        dialoguePanel.SetActive(false);
    }
    public void ShowDialogueUI(bool show)
    {
        dialoguePanel.SetActive(show);
    }

    public void SetNPCInfo(string npcName, Sprite portrait)
    {
        nameText.text = npcName;
        portraitImage.sprite = portrait;
    }

    public void SetDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void ClearChoices()
    {
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public GameObject CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
        choiceButton.GetComponentInChildren<TMPro.TMP_Text>().text = choiceText;
        choiceButton.GetComponent<Button>().onClick.AddListener(onClick);
        return choiceButton;
    }
}
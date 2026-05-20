using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NPC))]
public class QuestIndicator : MonoBehaviour
{
    [Header("Main Indicator (Màn hình chính)")]
    public GameObject indicatorChildObject;
    public Sprite spriteNotStarted;
    public Sprite spriteInProgress;
    public Sprite spriteCompleted;

    [Header("Minimap Indicator (Bản đồ nhỏ)")]
    public GameObject minimapIndicatorObject;
    public Color colorNotStarted = Color.yellow;
    public Color colorInProgress = Color.gray;
    public Color colorCompleted = new Color(1f, 0.5f, 0f);
    public Color colorNoQuest = Color.white;

    [Header("Cài đặt hiệu ứng đung đưa")]
    public float floatAmplitude = 0.02f;
    public float floatSpeed = 5f;

    private SpriteRenderer indicatorSpriteRenderer;
    private SpriteRenderer minimapSpriteRenderer;

    [SerializeField] private NPC npc;
    [SerializeField] private QuestHandler questHandler;

    private Vector3 initialLocalPosition;
    private Coroutine floatCoroutine;

    void Awake()
    {
        npc = GetComponent<NPC>();
        questHandler = GetComponent<QuestHandler>();

        if (indicatorChildObject != null)
        {
            indicatorSpriteRenderer = indicatorChildObject.GetComponent<SpriteRenderer>();
            initialLocalPosition = indicatorChildObject.transform.localPosition;
            indicatorChildObject.SetActive(false);
        }
        else
        {
            Debug.LogError("NPCQuestIndicator: Vui lòng gán 'indicatorChildObject'", this);
        }

        if (minimapIndicatorObject != null)
        {
            minimapSpriteRenderer = minimapIndicatorObject.GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (npc != null)
        {
            questHandler.OnQuestStateUpdated += UpdateIndicator;
            UpdateIndicator(questHandler.CurrentQuestState);
        }
    }

    private void OnDisable()
    {
        if (npc != null)
        {
            questHandler.OnQuestStateUpdated -= UpdateIndicator;
        }
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
        }
    }

    private void UpdateIndicator(QuestState state)
    {
        if (indicatorChildObject == null || indicatorSpriteRenderer == null) return;

        if (GameStateManager.IsDialogueActive ||
            npc.CurrentActiveDialogue == null ||
            npc.CurrentActiveDialogue.quest == null ||
            QuestController.Instance.IsQuestHandedIn(npc.CurrentActiveDialogue.quest.questID))
        {
            indicatorChildObject.SetActive(false);

            if (minimapIndicatorObject != null)
            {
                minimapIndicatorObject.SetActive(true);
                if (minimapSpriteRenderer != null) minimapSpriteRenderer.color = colorNoQuest;
            }

            StopFloatingEffect();
            return;
        }

        indicatorChildObject.SetActive(true);
        if (minimapIndicatorObject != null) minimapIndicatorObject.SetActive(true);
        StartFloatingEffect();

        switch (state)
        {
            case QuestState.NotStarted:
                indicatorSpriteRenderer.sprite = spriteNotStarted;
                if (minimapSpriteRenderer != null) minimapSpriteRenderer.color = colorNotStarted;
                break;

            case QuestState.InProgress:
                indicatorSpriteRenderer.sprite = spriteInProgress;
                if (minimapSpriteRenderer != null) minimapSpriteRenderer.color = colorInProgress;
                break;

            case QuestState.Completed:
                indicatorSpriteRenderer.sprite = spriteCompleted;
                if (minimapSpriteRenderer != null) minimapSpriteRenderer.color = colorCompleted;
                break;

            case QuestState.NoMoreQuests:
                indicatorChildObject.SetActive(false);

                if (minimapIndicatorObject != null)
                {
                    minimapIndicatorObject.SetActive(true);
                    if (minimapSpriteRenderer != null) minimapSpriteRenderer.color = colorNoQuest;
                }

                StopFloatingEffect();
                break;
        }
    }

    private void StartFloatingEffect()
    {
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
        }
        floatCoroutine = StartCoroutine(FloatIndicator());
    }

    private void StopFloatingEffect()
    {
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }
        if (indicatorChildObject != null)
        {
            indicatorChildObject.transform.localPosition = initialLocalPosition;
        }
    }

    private IEnumerator FloatIndicator()
    {
        while (true)
        {
            float newY = initialLocalPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            indicatorChildObject.transform.localPosition = new Vector3(initialLocalPosition.x, newY, initialLocalPosition.z);
            yield return null;
        }
    }
}
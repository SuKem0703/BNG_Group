using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NPC))]
public class NPCQuestIndicator : MonoBehaviour
{
    public GameObject indicatorChildObject;

    [Header("Sprites cho các trạng thái")]
    public Sprite spriteNotStarted;
    public Sprite spriteInProgress;
    public Sprite spriteCompleted;

    [Header("Cài đặt hiệu ứng đung đưa")]
    [Tooltip("Biên độ (khoảng cách) di chuyển lên xuống")]
    public float floatAmplitude = 0.02f;
    [Tooltip("Tốc độ di chuyển lên xuống")]
    public float floatSpeed = 5f;

    private SpriteRenderer indicatorSpriteRenderer;
    private NPC npc;

    private Vector3 initialLocalPosition;
    private Coroutine floatCoroutine;

    void Awake()
    {
        npc = GetComponent<NPC>();
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
    }

    private void OnEnable()
    {
        if (npc != null)
        {
            npc.OnQuestStateUpdated += UpdateIndicator;
            UpdateIndicator(npc.CurrentQuestState);
        }
    }

    private void OnDisable()
    {
        if (npc != null)
        {
            npc.OnQuestStateUpdated -= UpdateIndicator;
        }
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
        }
    }

    private void UpdateIndicator(NPC.QuestState state)
    {
        if (indicatorChildObject == null || indicatorSpriteRenderer == null) return;

        if (GameStateManager.IsDialogueActive)
        {
            indicatorChildObject.SetActive(false);
            StopFloatingEffect();
            return;
        }
        if (npc.CurrentActiveDialogue == null || npc.CurrentActiveDialogue.quest == null || QuestController.Instance.IsQuestHandedIn(npc.CurrentActiveDialogue.quest.questID))
        {
            indicatorChildObject.SetActive(false);
            StopFloatingEffect();
            return;
        }

        indicatorChildObject.SetActive(true);
        StartFloatingEffect();

        switch (state)
        {
            case NPC.QuestState.NotStarted:
                indicatorSpriteRenderer.sprite = spriteNotStarted;
                break;

            case NPC.QuestState.InProgress:
                indicatorSpriteRenderer.sprite = spriteInProgress;
                break;

            case NPC.QuestState.Completed:
                indicatorSpriteRenderer.sprite = spriteCompleted;
                break;

            case NPC.QuestState.NoMoreQuests:
                indicatorChildObject.SetActive(false);
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
            // Sử dụng Sine wave để tạo chuyển động mượt mà lên xuống
            float newY = initialLocalPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            indicatorChildObject.transform.localPosition = new Vector3(initialLocalPosition.x, newY, initialLocalPosition.z);
            yield return null; // Chờ 1 frame trước khi lặp lại
        }
    }
}
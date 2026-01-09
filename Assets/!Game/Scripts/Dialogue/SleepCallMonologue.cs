using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(999)]
[RequireComponent(typeof(Collider2D))]
public class SleepCallMonologue : Monologue
{
    [Header("Sleep Call Specifics")]
    [SerializeField] private Image _blackOverlay;

    [Header("Auto Trigger Settings")]
    [Tooltip("Nếu true, sẽ tự động chạy sau khi Load xong mà không cần chạm vào.")]
    public bool autoTriggerAfterLoad = true;

    private bool _hasStartedCutsceneMode = false;
    private bool _hasTriggered = false;

    protected override void Start()
    {
        isOneTimeOnly = true;
        base.Start();

        if (autoTriggerAfterLoad)
        {
            StartCoroutine(WaitAndAutoTrigger());
        }
    }

    private IEnumerator WaitAndAutoTrigger()
    {
        // 1. Chờ Save load xong
        yield return new WaitUntil(() => SaveController.IsDataLoaded);
        yield return null;

        if (this == null || !gameObject.activeInHierarchy)
            yield break;

        // 2. Chờ DialogueController
        yield return new WaitUntil(() => DialogueController.instance != null);

        // 3. Chờ các intro / cutscene KẾT THÚC HOÀN TOÀN
        yield return new WaitUntil(() => FindFirstObjectByType<ChapterIntroSequence>() == null);
        yield return new WaitUntil(() => FindFirstObjectByType<StoryScrollController>() == null);
        yield return new WaitUntil(() => FindFirstObjectByType<CameraPanIntro>() == null);

        // 4. Chờ MapController thoát cutscene
        if (MapController.Instance != null)
        {
            yield return new WaitUntil(() => !MapController.Instance.IsCutsceneMode);
        }

        // 5. Đảm bảo GameState sạch
        GameStateManager.IsDialogueActive = false;
        GameStateManager.EndLoading();

        yield return null; // buffer 1 frame

        if (this == null || !gameObject.activeInHierarchy || _hasTriggered)
            yield break;

        _hasTriggered = true;

        // 6. ÉP MỞ DIALOG – KHÔNG CHỜ CanInteract
        OpenDialogOnTrigger();
    }

    protected override void StartDialogue()
    {
        if (MapController.Instance != null)
        {
            MapController.Instance.IsCutsceneMode = true;
            _hasStartedCutsceneMode = true;
        }

        if (_blackOverlay != null)
        {
            _blackOverlay.color = Color.black;
            _blackOverlay.gameObject.SetActive(true);
        }

        base.StartDialogue();
    }

    public override void EndDialogue()
    {
        RestoreMapState();

        if (_blackOverlay != null)
        {
            Destroy(_blackOverlay.gameObject);
        }

        base.EndDialogue();
    }

    protected override void OnDestroy()
    {
        if (_hasStartedCutsceneMode)
        {
            RestoreMapState();
        }

        base.OnDestroy();
    }

    private void RestoreMapState()
    {
        if (MapController.Instance != null)
        {
            MapController.Instance.IsCutsceneMode = false;
            MapController.Instance.ShowMapNameUI();
        }

        _hasStartedCutsceneMode = false;
    }

    // OVERRIDE: Auto-trigger KHÔNG bị khóa bởi GameState cũ
    public override bool CanInteract()
    {
        if (!SaveController.IsDataLoaded) return false;
        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return false;
        return true;
    }
}

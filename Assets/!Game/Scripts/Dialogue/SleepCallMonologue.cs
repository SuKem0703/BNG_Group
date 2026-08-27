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
        yield return new WaitUntil(() => SaveController.IsDataLoaded);
        yield return null;

        if (this == null || !gameObject.activeInHierarchy)
            yield break;

        yield return new WaitUntil(() => DialogueController.instance != null);
        yield return new WaitUntil(() => FindFirstObjectByType<ChapterIntroSequence>() == null);
        yield return new WaitUntil(() => FindFirstObjectByType<StoryScrollController>() == null);
        yield return new WaitUntil(() => FindFirstObjectByType<CameraPanIntro>() == null);

        yield return new WaitUntil(() => !AreaController.isGlobalCutsceneMode);

        GameStateManager.IsDialogueActive = false;
        GameStateManager.EndLoading();

        yield return null;

        if (this == null || !gameObject.activeInHierarchy || _hasTriggered)
            yield break;

        _hasTriggered = true;

        OpenDialogOnTrigger();
    }

    protected override void StartDialogue()
    {
        AreaController.isGlobalCutsceneMode = true;
        _hasStartedCutsceneMode = true;

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
        AreaController.isGlobalCutsceneMode = false;
        if (AreaController.currentArea != null)
        {
            AreaController.currentArea.ShowMapNameUI();
        }

        _hasStartedCutsceneMode = false;
    }

    public override bool CanInteract()
    {
        if (!SaveController.IsDataLoaded) return false;
        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return false;
        return true;
    }
}
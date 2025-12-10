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

    protected override void Start()
    {
        isOneTimeOnly = true;

        base.Start();

        if (this != null && gameObject != null && autoTriggerAfterLoad)
        {
            StartCoroutine(WaitAndAutoTrigger());
        }
    }

    private IEnumerator WaitAndAutoTrigger()
    {
        yield return new WaitUntil(() => SaveController.IsDataLoaded);
        yield return null;

        if (this == null || gameObject == null) yield break;

        yield return new WaitUntil(() => DialogueController.instance != null);

        float timeout = 5f;
        while (GameStateManager.IsLoading && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitUntil(() => string.IsNullOrEmpty(SaveController.pendingSceneName));

        if (MapController.Instance != null)
        {
            yield return new WaitUntil(() => !MapController.Instance.IsCutsceneMode);
        }

        yield return new WaitForSeconds(0.2f);

        if (this == null || !gameObject.activeInHierarchy) yield break;

        // --- TRIGGER ---
        if (CanInteract())
        {
            OpenDialogOnTrigger();
        }
        else
        {
            Debug.Log("SleepCallMonologue: Cannot interact yet, waiting...");
            yield return new WaitUntil(() => CanInteract());
            OpenDialogOnTrigger();
        }
    }

    protected override void StartDialogue()
    {
        if (MapController.Instance != null)
        {
            MapController.Instance.IsCutsceneMode = true;
            _hasStartedCutsceneMode = true;
        }

        if (DialogueController.instance != null && _blackOverlay != null)
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
        if (_hasStartedCutsceneMode) RestoreMapState();
        base.OnDestroy();
    }

    private void RestoreMapState()
    {
        if (_hasStartedCutsceneMode && MapController.Instance != null)
        {
            MapController.Instance.IsCutsceneMode = false;
            MapController.Instance.ShowMapNameUI();
            _hasStartedCutsceneMode = false;
        }
    }
}
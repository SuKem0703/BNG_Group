using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(999)]
[RequireComponent(typeof(Collider2D))]
public class SleepCallMonologue : Monologue
{
    [SerializeField] private Image _blackOverlay;
    private bool _hasStartedCutsceneMode = false;

    protected override void Start()
    {
        isOneTimeOnly = true;

        base.Start();
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
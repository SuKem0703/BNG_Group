using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public enum PanTriggerMode
{
    PlayOnFirstLoad,
    ManualOnly
}

public class CameraPanIntro : MonoBehaviour
{
    [Header("Settings")]
    public PanTriggerMode triggerMode = PanTriggerMode.PlayOnFirstLoad;
    public float focusDuration = 2.0f;
    public float blendDuration = 2.0f;

    [Header("Save Logic")]
    public string introID;
    private string finalID;

    private bool isPlaying = false;

    private void Start()
    {
        finalID = !string.IsNullOrEmpty(introID) ? introID : GenerateDeterministicID();

        if (triggerMode == PanTriggerMode.PlayOnFirstLoad)
        {
            if (!SaveController.IsDataLoaded)
                SaveController.OnDataLoaded += HandleLoaded;
            else
                CheckSaveAndPlay();
        }
    }

    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleLoaded;
    }

    private void HandleLoaded()
    {
        SaveController.OnDataLoaded -= HandleLoaded;
        CheckSaveAndPlay();
    }

    private void CheckSaveAndPlay()
    {
        if (SaveController.Instance == null) return;

        if (!SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, finalID))
        {
            StartCoroutine(PlayIntroSequence(true));
        }
    }

    public void TriggerManualPan()
    {
        if (!isPlaying)
        {
            StartCoroutine(PlayIntroSequence(false));
        }
    }

    private IEnumerator PlayIntroSequence(bool markAsSaved)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) yield break;

        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient == null || localClient.PlayerObject == null) yield break;

        Transform playerTransform = localClient.PlayerObject.transform;

        var vCam = FindFirstObjectByType<CinemachineCamera>();
        if (vCam == null) yield break;

        isPlaying = true;

        var chapterIntro = FindFirstObjectByType<ChapterIntroSequence>();
        if (chapterIntro != null) yield return new WaitUntil(() => chapterIntro == null);

        var storyScroll = FindFirstObjectByType<StoryScrollController>();
        if (storyScroll != null) yield return new WaitUntil(() => storyScroll == null);

        GameStateManager.StartLoading();

        Transform originalTarget = vCam.Target.TrackingTarget;

        GameObject dummyTarget = new GameObject("DummyPanTarget");
        dummyTarget.transform.position = originalTarget != null ? originalTarget.position : playerTransform.position;

        vCam.Target.TrackingTarget = dummyTarget.transform;

        Vector3 startPos = dummyTarget.transform.position;
        Vector3 targetPos = transform.position;

        float elapsed = 0f;
        while (elapsed < blendDuration)
        {
            dummyTarget.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / blendDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        dummyTarget.transform.position = targetPos;

        yield return new WaitForSeconds(focusDuration);

        elapsed = 0f;
        while (elapsed < blendDuration)
        {
            Vector3 returnPos = originalTarget != null ? originalTarget.position : playerTransform.position;
            dummyTarget.transform.position = Vector3.Lerp(targetPos, returnPos, elapsed / blendDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        vCam.Target.TrackingTarget = originalTarget;
        Destroy(dummyTarget);

        GameStateManager.EndLoading();
        isPlaying = false;

        if (markAsSaved && SaveController.Instance != null)
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, finalID);
            SaveController.Instance.TriggerAutoSave();
        }
    }

    private string GenerateDeterministicID()
    {
        var p = transform.position;
        return $"{SceneManager.GetActiveScene().name}_CamPan_{Mathf.RoundToInt(p.x * 100)}_{Mathf.RoundToInt(p.y * 100)}";
    }
}
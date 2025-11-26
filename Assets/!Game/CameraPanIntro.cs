using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CameraPanIntro : MonoBehaviour
{
    [Header("Camera Config")]
    public CinemachineCamera targetCamera;

    [Header("Settings")]
    public float focusDuration = 2.0f;
    public float blendDuration = 2.0f;

    [Header("Save Logic")]
    public string introID;

    private string finalID;

    private void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<CinemachineCamera>();

        if (targetCamera != null)
        {
            var p = targetCamera.transform.position;
            p.z = -10f;
            targetCamera.transform.position = p;
        }

        if (!string.IsNullOrEmpty(introID)) finalID = introID;
        else finalID = GenerateDeterministicID();

        if (!SaveController.IsDataLoaded)
            SaveController.OnDataLoaded += HandleLoaded;
        else
            CheckSaveAndPlay();
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

        if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, finalID))
        {
            if (targetCamera != null) Destroy(targetCamera.gameObject);
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(PlayIntro());
        }
    }

    IEnumerator PlayIntro()
    {
        var chapterIntro = FindFirstObjectByType<ChapterIntroSequence>();
        if (chapterIntro != null)
        {
            yield return new WaitUntil(() => chapterIntro == null);
        }

        GameStateManager.StartLoading();

        if (targetCamera != null) targetCamera.Priority = 20;
        yield return new WaitForSeconds(blendDuration);

        yield return new WaitForSeconds(focusDuration);

        if (targetCamera != null) targetCamera.Priority = 0;
        yield return new WaitForSeconds(blendDuration);

        GameStateManager.EndLoading();

        if (SaveController.Instance != null)
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, finalID);
            SaveController.Instance.TriggerAutoSave();
        }

        if (targetCamera != null) Destroy(targetCamera.gameObject);
        Destroy(gameObject);
    }

    private string GenerateDeterministicID()
    {
        var p = transform.position;
        return $"{SceneManager.GetActiveScene().name}_CamPan_{Mathf.RoundToInt(p.x * 100)}_{Mathf.RoundToInt(p.y * 100)}";
    }
}
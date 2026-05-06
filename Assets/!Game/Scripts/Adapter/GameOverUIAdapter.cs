using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class GameOverUIAdapter : MonoBehaviour
{
    public static GameOverUIAdapter Instance { get; private set; }

    [Header("UI Animation Settings")]
    public float fadeDuration = 1.0f;

    private CanvasGroup canvasGroup;
    private bool isRespawning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    private void OnEnable() => DeathService.OnPlayerDied += HideCommonUI;
    private void OnDisable() => DeathService.OnPlayerDied -= HideCommonUI;

    private void HideCommonUI()
    {
        if (CommonUIController.Instance != null)
            CommonUIController.Instance.SetUIVisible(false);
    }

    public void ShowGameOverUI()
    {
        PauseController.SetPause(true);
        HideCommonUI();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(true);

        canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true).OnComplete(() =>
        {
            canvasGroup.blocksRaycasts = true;
        });
    }

    public void OnRespawnClicked()
    {
        if (isRespawning) return;
        isRespawning = true;

        PauseController.SetPause(false);
        DOTween.KillAll();

        canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            isRespawning = false;
        });

        if (DeathService.Instance != null)
        {
            DeathService.Instance.ExecuteRespawn();
        }
    }
}
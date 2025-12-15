using UnityEngine;
using System.Collections;

public class MenuStateManager : MonoBehaviour
{
    public static MenuStateManager Instance { get; private set; }

    private GameObject currentActiveMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnDestroy()
    {
        ResetState();

        if (Instance == this) Instance = null;
    }

    // Open a specified menu panel
    public void OpenMenu(GameObject menuPanel, bool shouldPause = true)
    {
        if (currentActiveMenu != null && currentActiveMenu != menuPanel)
        {
            currentActiveMenu.SetActive(false);
        }

        currentActiveMenu = menuPanel;

        if (menuPanel != null)
            menuPanel.SetActive(true);

        GameStateManager.IsMenuOpen = true;
        GameStateManager.CanOpenMenu = false;

        if (shouldPause)
            PauseController.SetPause(true);

        CommonUIController.Instance?.SetUIVisible(false);
    }

    // Close the currently active menu
    public void CloseCurrentMenu()
    {
        if (currentActiveMenu != null)
        {
            currentActiveMenu.SetActive(false);
            currentActiveMenu = null;
        }

        GameStateManager.IsMenuOpen = false;

        StartCoroutine(EnableMenuAccessRoutine());

        PauseController.SetPause(false);

        CommonUIController.Instance?.SetUIVisible(true);
        //GameStateManager.EndLoading();
    }
    IEnumerator EnableMenuAccessRoutine()
    {
        yield return null;

        GameStateManager.CanOpenMenu = true;
    }

    public void ResetState()
    {
        // Reset global states to avoid stale pause/loading blocking input after scene change
        PauseController.SetPause(false);
        Time.timeScale = 1f;
        GameStateManager.IsMenuOpen = false;
        GameStateManager.CanOpenMenu = true;
        // Ensure loading flag cleared
        GameStateManager.EndLoading();
    }

    public bool IsAnyMenuOpen() => currentActiveMenu != null;
}
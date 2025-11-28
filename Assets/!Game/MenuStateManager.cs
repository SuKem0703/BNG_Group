using UnityEngine;

public class MenuStateManager : MonoBehaviour
{
    public static MenuStateManager Instance { get; private set; }

    private GameObject currentActiveMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void OpenMenu(GameObject menuPanel, bool shouldPause = true)
    {
        if (currentActiveMenu != null && currentActiveMenu != menuPanel)
        {
            CloseCurrentMenu();
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
    public void CloseCurrentMenu()
    {
        if (currentActiveMenu != null)
        {
            currentActiveMenu.SetActive(false);
            currentActiveMenu = null;
        }

        GameStateManager.IsMenuOpen = false;
        GameStateManager.CanOpenMenu = true;

        PauseController.SetPause(false);

        CommonUIController.Instance?.SetUIVisible(true);
    }

    public bool IsAnyMenuOpen() => currentActiveMenu != null;
}
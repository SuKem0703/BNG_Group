using UnityEngine;

public static class GameStateManager
{
    public static bool IsLoading { get; private set; } = false;
    public static bool IsDialogueActive { get; set; } = false;
    public static bool CanOpenMenu { get; set; } = true;
    public static bool IsMenuOpen { get; set; } = false;
    public static void StartLoading()
    {
        IsLoading = true;
        CanOpenMenu = false;
        PauseController.SetPause(true);
    }
    public static void EndLoading()
    {
        if (IsDialogueActive) return;
        IsLoading = false;
        CanOpenMenu = true;
        PauseController.SetPause(false);
    }
    public static bool CanProcessInput()
    {
        if (IsLoading) return false;
        if (IsDialogueActive) return false;
        if (PauseController.IsGamePause) return false;
        return true;
    }
}

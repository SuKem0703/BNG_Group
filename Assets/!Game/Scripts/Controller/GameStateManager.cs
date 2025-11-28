using UnityEngine;

public static class GameStateManager
{
    // --- CÁC TRẠNG THÁI GAME (FLAGS) ---
    public static bool IsLoading { get; private set; } = false;
    public static bool IsDialogueActive { get; set; } = false;
    public static bool IsMenuOpen { get; set; } = false;
    public static bool IsCutsceneActive { get; set; } = false;

    // Biến này để chặn mở menu trong một số trường hợp đặc biệt (ví dụ Tutorial)
    public static bool CanOpenMenu { get; set; } = true;

    // --- HÀM ĐIỀU KHIỂN TRẠNG THÁI LOADING ---
    public static void StartLoading()
    {
        IsLoading = true;
        CanOpenMenu = false;
        PauseController.SetPause(true);
    }

    public static void EndLoading()
    {
        IsLoading = false;

        if (!IsDialogueActive && !IsCutsceneActive)
        {
            CanOpenMenu = true;
            PauseController.SetPause(false);
        }
    }

    // --- HÀM KIỂM TRA INPUT ---
    public static bool CanProcessInput()
    {
        if (IsLoading) return false;

        if (IsDialogueActive || IsCutsceneActive) return false;

        if (IsMenuOpen) return false;

        if (PauseController.IsGamePause) return false;

        return true;
    }
}
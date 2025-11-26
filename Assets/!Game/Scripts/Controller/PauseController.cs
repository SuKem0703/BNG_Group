using UnityEngine;

/// <summary>
/// Quản lý trạng thái pause của game.
/// </summary>
public class PauseController : MonoBehaviour
{
    public static bool IsManualPause { get; private set; } = false;
    public static bool IsFocusPause { get; private set; } = false;
    public static bool IsGamePause => IsManualPause || IsFocusPause;

    public static void SetPause(bool pause) => IsManualPause = pause;

    void OnApplicationFocus(bool hasFocus)
    {
        IsFocusPause = !hasFocus;

        if (hasFocus)
        {
            ServerTimeManager fetcher = FindFirstObjectByType<ServerTimeManager>();
            if (fetcher != null)
            {
                StartCoroutine(fetcher.FetchServerTime());
            }
        }
    }
}

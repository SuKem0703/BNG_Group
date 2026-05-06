using UnityEngine;

public class PauseController : MonoBehaviour
{
    public static bool IsManualPause { get; private set; } = false;
    public static bool IsFocusPause { get; private set; } = false;

    public static bool IsGamePause
    {
        get
        {
            if (CoopManager.IsCoop)
            {
                return false;
            }

            return IsManualPause || IsFocusPause;
        }
    }

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
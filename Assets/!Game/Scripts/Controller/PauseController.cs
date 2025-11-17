using UnityEngine;

public class PauseController : MonoBehaviour
{
    // Pause do người chơi bấm (manual)
    public static bool IsManualPause { get; private set; } = false;

    // Pause do mất focus cửa sổ
    public static bool IsFocusPause { get; private set; } = false;

    // Pause tổng
    public static bool IsGamePause => IsManualPause || IsFocusPause;

    // Hàm này thay cho SetPause cũ, nhưng rõ nghĩa hơn
    public static void SetPause(bool pause)
    {
        IsManualPause = pause;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        IsFocusPause = !hasFocus;

        if (hasFocus)
        {
            ServerTimeFetcher fetcher = FindFirstObjectByType<ServerTimeFetcher>();
            if (fetcher != null)
            {
                StartCoroutine(fetcher.FetchServerTime());
            }
            else
            {
                Debug.LogWarning("Không tìm thấy ServerTimeFetcher!");
            }
        }
    }
}

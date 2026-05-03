using UnityEngine;
using Unity.Netcode;

public class PauseController : MonoBehaviour
{
    public static bool IsManualPause { get; private set; } = false;
    public static bool IsFocusPause { get; private set; } = false;

    public static bool IsGamePause
    {
        get
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count > 1)
                {
                    return false;
                }
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
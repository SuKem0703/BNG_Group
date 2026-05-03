using Unity.Netcode;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{
    private static DontDestroyOnLoad instance;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBoot()
    {
        if (instance == null)
        {
            GameObject corePrefab = Resources.Load<GameObject>("CoreManagers");
            if (corePrefab != null)
            {
                Instantiate(corePrefab);
            }

            if (NetworkManager.Singleton == null)
            {
                GameObject netPrefab = Resources.Load<GameObject>("NetworkManager");
                if (netPrefab != null) Instantiate(netPrefab);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStartHostForDev()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
            return;

        if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Debug.Log("<color=green>[Dev Mode]</color> Tự động StartHost()!");
            Unity.Netcode.NetworkManager.Singleton.StartHost();
        }
    }
#endif

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}
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
            Debug.Log("<color=green>[Dev Mode]</color> Chuẩn bị tự động StartHost()...");

            ushort dynamicPort = 7777;
            try
            {
                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp))
                {
                    socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0));
                    dynamicPort = (ushort)((System.Net.IPEndPoint)socket.LocalEndPoint).Port;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Dev Mode] Không thể xin Port động. Lỗi: {e.Message}");
            }

            var transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("127.0.0.1", dynamicPort, "0.0.0.0");
                Debug.Log($"<color=green>[Dev Mode]</color> Đã gán Port tự động: {dynamicPort} để tránh trùng lặp!");
            }

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
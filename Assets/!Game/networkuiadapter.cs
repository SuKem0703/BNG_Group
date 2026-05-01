using Unity.Netcode;
using UnityEngine;

public class networkuiadapter : MonoBehaviour
{
    public void StartServer()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartServer();
            // gameObject.SetActive(false);
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            // gameObject.SetActive(false);
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            // gameObject.SetActive(false);
        }
    }
}
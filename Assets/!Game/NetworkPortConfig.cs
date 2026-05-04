using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[RequireComponent(typeof(UnityTransport))]
public class NetworkPortConfig : MonoBehaviour
{
    private UnityTransport transport;

    private void Awake()
    {
        transport = GetComponent<UnityTransport>();
    }

    public static ushort GetAvailableUdpPort()
    {
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }

    public void StartHostWithDynamicPort()
    {
        ushort dynamicPort = GetAvailableUdpPort();

        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", dynamicPort, "0.0.0.0");
            Debug.Log($"[Network] Đã gán thành công Port ngẫu nhiên cho Host: {dynamicPort}");
        }
        else
        {
            Debug.LogError("[Network] Không tìm thấy UnityTransport!");
            return;
        }

        NetworkManager.Singleton.StartHost();
    }

    public void JoinGameAsClient(string ipAddress, ushort hostPort)
    {
        if (transport != null)
        {
            transport.SetConnectionData(ipAddress, hostPort);
            NetworkManager.Singleton.StartClient();
        }
    }
}
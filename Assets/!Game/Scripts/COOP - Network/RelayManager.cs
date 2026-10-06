using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

[RequireComponent(typeof(UnityTransport))]
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private bool isSigningIn = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ========================================================
    // KIỂM TRA MẠNG & KHỞI TẠO UNITY SERVICES
    // ========================================================
    public bool CheckInternetConnection()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private async Task<bool> EnsureUnityServicesInitialized()
    {
        if (isSigningIn)
        {
            Debug.LogWarning("[Relay] Đang trong quá trình đăng nhập. Vui lòng chờ...");
            return false;
        }

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                isSigningIn = true;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                isSigningIn = false;
                Debug.Log($"[Relay] Đã kết nối Unity Services với ID: {AuthenticationService.Instance.PlayerId}");
            }
            return true;
        }
        catch (Exception e)
        {
            isSigningIn = false;
            Debug.LogError($"[Relay] Khởi tạo Unity Services gặp vấn đề: {e.Message}");
            return false;
        }
    }

    // ========================================================
    // LOGIC INTERNET (UNITY RELAY)
    // ========================================================
    public async Task<string> CreateRelayHost(int maxPlayers = 3)
    {
        if (!CheckInternetConnection())
        {
            Debug.LogError("[Relay] Không có kết nối Internet.");
            return null;
        }

        bool initialized = await EnsureUnityServicesInitialized();
        if (!initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[Relay] Không thể kết nối với dịch vụ xác thực của Unity.");
            return null;
        }

        try
        {
            CachePlayerPosition();

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(500);
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Lỗi tạo phòng: {e.Message}");
            return null;
        }
    }

    private void CachePlayerPosition()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            SaveController.nextSpawnPosition = NetworkManager.Singleton.LocalClient.PlayerObject.transform.position;
        }
        else
        {
            GameObject oldPlayer = GameObject.FindGameObjectWithTag("PlayerController");
            if (oldPlayer == null) oldPlayer = GameObject.FindGameObjectWithTag("Player");
            if (oldPlayer != null) SaveController.nextSpawnPosition = oldPlayer.transform.position;
        }
    }

    public async Task<bool> JoinRelayClient(string joinCode)
    {
        if (!CheckInternetConnection())
        {
            Debug.LogError("[Relay] Không có kết nối Internet.");
            return false;
        }

        bool initialized = await EnsureUnityServicesInitialized();
        if (!initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[Relay] Không thể kết nối với dịch vụ xác thực của Unity.");
            return false;
        }

        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(500);
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Lỗi tham gia phòng: {e.Message}");
            return false;
        }
    }

    // ========================================================
    // LOGIC MẠNG NỘI BỘ (LAN) - Giữ nguyên không cần Internet
    // ========================================================
    public async Task<(bool success, string ip, ushort port)> StartLANHost()
    {
        CachePlayerPosition();

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await Task.Delay(500);
        }

        string localIP = GetLocalIPAddress();
        ushort dynamicPort = GetAvailableUdpPort();

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(localIP, dynamicPort, "0.0.0.0");

        NetworkManager.Singleton.StartHost();
        return (true, localIP, dynamicPort);
    }

    public async Task<bool> JoinLANClient(string ipAddress, ushort port)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            await Task.Delay(500);
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ipAddress, port);

        NetworkManager.Singleton.StartClient();
        return true;
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public static ushort GetAvailableUdpPort()
    {
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
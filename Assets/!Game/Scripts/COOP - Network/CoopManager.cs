using Unity.Netcode;

public static class CoopManager
{
    public static bool IsCoop
    {
        get
        {
            if (NetworkManager.Singleton == null) return false;

            if (NetworkManager.Singleton.IsServer)
            {
                return NetworkManager.Singleton.ConnectedClients.Count > 1;
            }

            return NetworkManager.Singleton.IsConnectedClient;
        }
    }

    public static int PlayerCount
    {
        get
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                return NetworkManager.Singleton.ConnectedClients.Count;
            }
            return 1;
        }
    }

    public static float GetEnemyStatMultiplier()
    {
        int count = PlayerCount;
        if (count <= 1) return 1.0f;     // Solo: Không buff
        if (count == 2) return 1.5f;     // 2 người: Tăng 50%
        if (count == 3) return 2.0f;     // 3 người: Tăng 100%
        return 2.5f;                     // 4 người: Tăng 150%
    }
}
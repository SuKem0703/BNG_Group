using UnityEngine;

public static class NetworkConfig
{
    public const string BASE_URL = "https://chronicles-of-knight-and-mage.onrender.com";

    // public const string BASE_URL = "http://localhost:5000"; 
    public static string GetUrl(string endpoint)
    {
        if (endpoint.StartsWith("/")) endpoint = endpoint.Substring(1);
        return $"{BASE_URL}/{endpoint}";
    }
}
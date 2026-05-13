using UnityEngine;

public static class NetworkConfig
{
    //public const string BASE_URL = "https://chronicles-of-knight-and-mage.onrender.com";

    public const string BASE_URL = "http://18.141.153.201:8080"; 
    public static string GetUrl(string endpoint)
    {
        if (endpoint.StartsWith("/")) endpoint = endpoint.Substring(1);
        return $"{BASE_URL}/{endpoint}";
    }
}
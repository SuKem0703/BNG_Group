using UnityEngine;

public static class NetworkConfig
{
    public const string BASE_URL = "http://127.0.0.1:8080/api";

    // public const string BASE_URL = "https://l1fbbhusal.execute-api.ap-southeast-1.amazonaws.com/";

    public static string GetUrl(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return BASE_URL;

        endpoint = endpoint.Trim();
        while (endpoint.StartsWith("/", System.StringComparison.Ordinal))
        {
            endpoint = endpoint.Substring(1);
        }

        if (endpoint.StartsWith("api/", System.StringComparison.OrdinalIgnoreCase))
        {
            endpoint = endpoint.Substring(4);
        }

        return $"{BASE_URL}/{endpoint}";
    }
}
using System.Collections;
using UnityEngine;

public static class WaitForPlayer
{
    public static IEnumerator GetPlayerStats(System.Action<PlayerStats> callback)
    {
        PlayerStats playerStats = null;

        while (playerStats == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerController");

            if (playerObj != null)
            {
                playerStats = playerObj.GetComponent<PlayerStats>();
            }

            if (playerStats == null)
                yield return null;
        }

        callback?.Invoke(playerStats);
    }
}

using System;
using System.Reflection;
using UnityEngine;

// Runtime helper: put outside an Editor-only folder or assembly.
// Player 1 = Main Editor. Player 2/3/4 = MPPM virtual players.
public static class DevClientIdentity
{
    public static int GetPlayerIndex()
    {
#if UNITY_EDITOR
        string[] args = Environment.GetCommandLineArgs();

        // Explicit override for unusual MPPM scenarios:
        // -ckm-slot 2 (or -ckm-slot=2)
        int overrideSlot = ReadArgumentInt(args, "-ckm-slot");
        if (overrideSlot >= 1 && overrideSlot <= 4)
            return overrideSlot;

        // The documented MPPM identity argument: -name Player2/3/4.
        string playerName = ReadArgumentValue(args, "-name");
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            playerName = playerName.Trim().Trim('"');
            if (playerName.StartsWith("Player", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(playerName.Substring("Player".Length), out int index) &&
                index >= 1 && index <= 4)
                return index;
        }

        // In newer MPPM versions we can determine main vs virtual even
        // when the -name argument is not available. Reflection keeps the
        // helper compatible with the different Unity package namespaces.
        bool? isMainEditor = GetIsMainEditor();
        if (isMainEditor == true)
            return 1;

        if (isMainEditor == false)
        {
            Debug.LogWarning(
                "[DevIdentity] Virtual Player detected, but -name is missing. " +
                "Using slot 2 for a TWO-PLAYER test only. For Player 3/4, " +
                "provide -ckm-slot 3/4 via MPPM launch arguments.");
            return 2;
        }

        // Older package fallback: clones normally live under Library/VP.
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (dataPath.IndexOf("/Library/VP/", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogWarning(
                "[DevIdentity] Virtual project detected from Library/VP, " +
                "but instance name was unavailable. Using slot 2 for TWO-PLAYER test only.");
            return 2;
        }
#endif
        return 1;
    }

#if UNITY_EDITOR
    private static string ReadArgumentValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return i + 1 < args.Length ? args[i + 1] : null;

            string prefix = key + "=";
            if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return args[i].Substring(prefix.Length);
        }
        return null;
    }

    private static int ReadArgumentInt(string[] args, string key)
    {
        return int.TryParse(ReadArgumentValue(args, key), out int value) ? value : 0;
    }

    // MPPM 1.x: Unity.Multiplayer.Playmode.CurrentPlayer
    // MPPM 2.x: Unity.Multiplayer.PlayMode.CurrentPlayer
    private static bool? GetIsMainEditor()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType("Unity.Multiplayer.PlayMode.CurrentPlayer", false)
                     ?? assembly.GetType("Unity.Multiplayer.Playmode.CurrentPlayer", false);
            if (type == null) continue;

            PropertyInfo property = type.GetProperty(
                "IsMainEditor", BindingFlags.Public | BindingFlags.Static);
            if (property == null || property.PropertyType != typeof(bool))
                continue;

            try
            {
                return (bool)property.GetValue(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[DevIdentity] Cannot read IsMainEditor: " + exception.Message);
            }
        }
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void DebugDevIdentity()
    {
        bool? isMainEditor = GetIsMainEditor();
        int slot = GetPlayerIndex();
        string commandLine = string.Join(" ", Environment.GetCommandLineArgs());
        int processId = System.Diagnostics.Process.GetCurrentProcess().Id;

        Debug.Log(
            $"[DevIdentity] PID={processId} | " +
            $"IsMainEditor={(isMainEditor.HasValue ? isMainEditor.Value.ToString() : "Unknown")} | " +
            $"Player Index={slot}\n" +
            $"[DevIdentity] Args={commandLine}\n" +
            $"[DevIdentity] DataPath={Application.dataPath}\n" +
            $"[DevIdentity] PersistentDataPath={Application.persistentDataPath}");
    }
#endif
}

using UnityEngine;

public static class AntiSpam
{
    private static float lastActionTime = 0f;

    public const float COOLDOWN_TIME = 0.3f;

    public static bool CanPerformAction()
    {
        if (Time.time - lastActionTime >= COOLDOWN_TIME)
        {
            lastActionTime = Time.time;
            return true;
        }
        Debug.LogWarning($"Hành động bị chặn vì đang trong thời gian cooldown ({COOLDOWN_TIME} giây). Vui lòng chờ.");
        return false;
    }
}
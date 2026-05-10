using UnityEngine;

public static class GameNotify
{
    private static NotifyUIController currentInstance;

    public static void Show(string message)
    {
        if (currentInstance == null)
        {
            if (LoadResourceManager.Instance == null)
            {
                Debug.LogWarning("[GameNotify] LoadResourceManager chưa được khởi tạo!");
                return;
            }

            GameObject prefab = LoadResourceManager.Instance.NotifyUIPrefab;
            if (prefab == null) return;

            GameObject notifyObj = Object.Instantiate(prefab);
            currentInstance = notifyObj.GetComponent<NotifyUIController>();

            if (currentInstance == null)
            {
                Debug.LogWarning($"[GameNotify] Prefab thiếu component NotifyUIController!");
                Object.Destroy(notifyObj);
                return;
            }
        }

        currentInstance.Show(message);
    }
}
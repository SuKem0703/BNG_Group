using UnityEngine;

public static class GameNotify
{
    public static void Show(string message)
    {
        if (LoadResourceManager.Instance == null)
        {
            Debug.LogWarning("[GameNotify] LoadResourceManager chưa được khởi tạo!");
            return;
        }

        GameObject prefab = LoadResourceManager.Instance.NotifyUIPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[GameNotify] NotifyUIPrefab bị null trong LoadResourceManager!");
            return;
        }

        GameObject notifyObj = Object.Instantiate(prefab);
        var notifyCtrl = notifyObj.GetComponent<NotifyUIController>();

        if (notifyCtrl != null)
        {
            notifyCtrl.Show(message);
        }
        else
        {
            Debug.LogWarning($"[GameNotify] Prefab '{notifyObj.name}' thiếu component NotifyUIController!");
            Object.Destroy(notifyObj);
        }
    }
}
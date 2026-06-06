using System.Collections.Generic;
using UnityEngine;

public class DamagePopupPool : MonoBehaviour
{
    public static DamagePopupPool Instance { get; private set; }

    [Header("Settings")]
    public DamagePopup popupPrefab;
    public int initialPoolSize = 20;

    private Queue<DamagePopup> pool = new Queue<DamagePopup>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPopup();
        }
    }

    private DamagePopup CreateNewPopup()
    {
        DamagePopup popup = Instantiate(popupPrefab, transform);
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
        return popup;
    }

    public DamagePopup GetPopup(Vector3 position)
    {
        if (pool.Count == 0)
        {
            CreateNewPopup();
        }

        DamagePopup popup = pool.Dequeue();
        popup.transform.position = position;
        popup.gameObject.SetActive(true);
        return popup;
    }

    public void ReturnPopup(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
    }
}
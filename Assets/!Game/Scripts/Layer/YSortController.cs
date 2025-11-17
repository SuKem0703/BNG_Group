using System;
using UnityEngine;

public class YSortController : MonoBehaviour
{
    public static event Action<float> OnPlayerYChanged;

    public static YSortController Instance { get; private set; }

    [Tooltip("Tag of player object to watch")]
    public string playerTag = "PlayerController";

    [Tooltip("Minimum delta Y to trigger updates")]
    public float minDeltaY = 0.01f;

    private Transform playerTransform;
    private float lastY = float.NaN;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var player = GameObject.FindWithTag(playerTag);
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player != null)
                playerTransform = player.transform;
            else
                return;
        }

        float y = playerTransform.position.y;
        if (float.IsNaN(lastY) || Mathf.Abs(y - lastY) >= minDeltaY)
        {
            lastY = y;
            OnPlayerYChanged?.Invoke(y);
        }
    }

    public void ForceUpdate()
    {
        if (playerTransform != null)
            OnPlayerYChanged?.Invoke(playerTransform.position.y);
    }
}

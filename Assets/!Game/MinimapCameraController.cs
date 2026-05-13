using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Camera))]
public class MinimapCameraController : MonoBehaviour
{
    [Header("Settings")]
    public float zOffset = -10f;

    [Tooltip("Tốc độ bám theo nhân vật")]
    public float followSmoothness = 10f;

    [Header("Map Bounds")]
    [Tooltip("Gắn Collider giới hạn bản đồ vào đây (BoxCollider2D hoặc PolygonCollider2D)")]
    public Collider2D mapBounds;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera minimapCamera;
    private bool instantSnapOnFirstFound = true;

    private void Awake()
    {
        if (minimapCamera == null)
            minimapCamera = GetComponent<Camera>();

        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.orthographic = true;
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
        {
            FindLocalPlayer();
            if (playerTransform == null) return;
        }

        Vector3 targetPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, zOffset);

        targetPosition = ClampCameraToBounds(targetPosition);

        if (instantSnapOnFirstFound)
        {
            transform.position = targetPosition;
            instantSnapOnFirstFound = false;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSmoothness);
        }
    }

    private Vector3 ClampCameraToBounds(Vector3 targetPos)
    {
        if (mapBounds == null || minimapCamera == null) return targetPos;

        Bounds bounds = mapBounds.bounds;

        float camHeight = minimapCamera.orthographicSize;
        float camWidth = camHeight * minimapCamera.aspect;

        float minX = bounds.min.x + camWidth;
        float maxX = bounds.max.x - camWidth;
        float minY = bounds.min.y + camHeight;
        float maxY = bounds.max.y - camHeight;

        if (minX > maxX)
        {
            minX = bounds.center.x;
            maxX = bounds.center.x;
        }
        if (minY > maxY)
        {
            minY = bounds.center.y;
            maxY = bounds.center.y;
        }

        float clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPos.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPos.z);
    }

    private void FindLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient != null && localClient.PlayerObject != null)
            {
                playerTransform = localClient.PlayerObject.transform;
                return;
            }
        }

        if (PlayerStats.Instance != null)
        {
            playerTransform = PlayerStats.Instance.transform;
            return;
        }

        GameObject p = GameObject.FindGameObjectWithTag("PlayerController");

        if (p != null)
        {
            playerTransform = p.transform;
        }
    }

    public void ResetTarget()
    {
        playerTransform = null;
        instantSnapOnFirstFound = true;
    }

    public void SetMapBounds(Collider2D newBounds)
    {
        mapBounds = newBounds;
    }
}
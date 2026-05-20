using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    [Header("Basic Settings")]
    public float defaultSize = 5f;
    public float maxSize = 10f;
    public float minSize = 2f;
    public float zoomSpeed = 2f;
    public float smoothTime = 0.2f;

    [Header("Dynamic Border Zoom")]
    [Tooltip("Bật tính năng tự zoom ra khi gần tường")]
    public bool enableBorderZoom = true;
    [Tooltip("Khoảng cách từ tường bắt đầu bị zoom ra")]
    public float borderThreshold = 3f;
    [Tooltip("Độ zoom bắt buộc khi chạm sát tường")]
    public float sizeAtBorder = 5f;

    [Header("Minimap")]
    public Camera minimapCamera;
    public float minimapZOffset = -50f;
    public float minimapSmoothness = 10f;
    private bool instantSnapMinimap = true;

    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform playerTransform;

    public CinemachineConfiner2D confiner;
    public Collider2D mapCollider;

    private float userDesiredSize;
    private float finalTargetSize;
    private float currentVelocity;

    private Vector2 debugClosestPoint;

    void Start()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineCamera>() ?? FindFirstObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
            userDesiredSize = virtualCamera.Lens.OrthographicSize;

            if (confiner != null)
            {
                confiner.Damping = 0f;
                mapCollider = confiner.BoundingShape2D;
            }
        }
        else
        {
            userDesiredSize = defaultSize;
        }

        if (minimapCamera != null)
        {
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.orthographic = true;
        }
    }

    void LateUpdate()
    {
        if (virtualCamera == null) return;

        if (playerTransform == null)
        {
            FindLocalPlayer();
            if (playerTransform == null) return;
        }

        ProcessMainCamera();
        ProcessMinimapCamera();
    }

    private void ProcessMainCamera()
    {
        float mapLimit = CalculateMaxOrthoSizeFromBound();
        float safeMaxSize = Mathf.Max(minSize, Mathf.Min(maxSize, mapLimit - 0.05f));

        if (GameStateManager.CanProcessInput())
        {
            float scrollData = Input.GetAxis("Mouse ScrollWheel");
            if (scrollData != 0f)
            {
                userDesiredSize -= scrollData * zoomSpeed;
            }
        }

        userDesiredSize = Mathf.Clamp(userDesiredSize, minSize, safeMaxSize);
        finalTargetSize = userDesiredSize;

        if (enableBorderZoom && mapCollider != null && playerTransform != null)
        {
            float distToBorder = GetDistanceToClosestBorder(playerTransform.position, out debugClosestPoint);

            if (distToBorder < borderThreshold)
            {
                float t = distToBorder / borderThreshold;
                float borderOverrideSize = Mathf.Lerp(sizeAtBorder, userDesiredSize, t);

                finalTargetSize = Mathf.Max(userDesiredSize, borderOverrideSize);
                finalTargetSize = Mathf.Min(finalTargetSize, safeMaxSize);
            }
        }

        float currentSize = virtualCamera.Lens.OrthographicSize;
        if (Mathf.Abs(currentSize - finalTargetSize) > 0.001f)
        {
            float newSize = Mathf.SmoothDamp(currentSize, finalTargetSize, ref currentVelocity, smoothTime);
            if (newSize > safeMaxSize)
            {
                newSize = safeMaxSize;
                currentVelocity = 0f;
            }
            virtualCamera.Lens.OrthographicSize = newSize;

            if (confiner != null)
            {
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }

    private void ProcessMinimapCamera()
    {
        if (minimapCamera == null || playerTransform == null) return;

        Vector3 targetPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, minimapZOffset);

        if (mapCollider != null)
        {
            Bounds bounds = mapCollider.bounds;
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

            float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

            targetPosition = new Vector3(clampedX, clampedY, minimapZOffset);
        }

        if (instantSnapMinimap)
        {
            minimapCamera.transform.position = targetPosition;
            instantSnapMinimap = false;
        }
        else
        {
            minimapCamera.transform.position = Vector3.Lerp(minimapCamera.transform.position, targetPosition, Time.deltaTime * minimapSmoothness);
        }
    }

    private void FindLocalPlayer()
    {
        Transform oldTransform = playerTransform;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient != null && localClient.PlayerObject != null)
            {
                playerTransform = localClient.PlayerObject.transform;
            }
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("PlayerController");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform != null)
        {
            if (virtualCamera != null) virtualCamera.Target.TrackingTarget = playerTransform;

            if (oldTransform != playerTransform) instantSnapMinimap = true;
        }
    }

    private float GetDistanceToClosestBorder(Vector2 point, out Vector2 closestPointOnEdge)
    {
        float minDst = float.MaxValue;
        closestPointOnEdge = point;

        if (mapCollider == null) return float.MaxValue;

        if (mapCollider is BoxCollider2D box)
        {
            Vector2 localPoint = box.transform.InverseTransformPoint(point);
            float halfWidth = box.size.x / 2f;
            float halfHeight = box.size.y / 2f;

            float distRight = (box.offset.x + halfWidth) - localPoint.x;
            float distLeft = localPoint.x - (box.offset.x - halfWidth);
            float distTop = (box.offset.y + halfHeight) - localPoint.y;
            float distBottom = localPoint.y - (box.offset.y - halfHeight);

            minDst = Mathf.Min(distRight, distLeft, distTop, distBottom);

            Vector2 closestLocal = localPoint;
            if (minDst == distRight) closestLocal.x = box.offset.x + halfWidth;
            else if (minDst == distLeft) closestLocal.x = box.offset.x - halfWidth;
            else if (minDst == distTop) closestLocal.y = box.offset.y + halfHeight;
            else if (minDst == distBottom) closestLocal.y = box.offset.y - halfHeight;

            closestPointOnEdge = box.transform.TransformPoint(closestLocal);
            return minDst;
        }

        else if (mapCollider is PolygonCollider2D poly)
        {
            for (int i = 0; i < poly.pathCount; i++)
            {
                Vector2[] pathPoints = poly.GetPath(i);

                for (int j = 0; j < pathPoints.Length; j++)
                {
                    Vector2 p1 = poly.transform.TransformPoint(pathPoints[j]);
                    Vector2 p2 = poly.transform.TransformPoint(pathPoints[(j + 1) % pathPoints.Length]);

                    Vector2 closest = GetClosestPointOnSegment(point, p1, p2);
                    float dst = Vector2.Distance(point, closest);

                    if (dst < minDst)
                    {
                        minDst = dst;
                        closestPointOnEdge = closest;
                    }
                }
            }
        }

        return minDst;
    }

    private Vector2 GetClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float magnitudeAB = ab.sqrMagnitude;
        float ABAPproduct = Vector2.Dot(ap, ab);
        float distance = ABAPproduct / magnitudeAB;

        if (distance < 0) return a;
        if (distance > 1) return b;
        return a + ab * distance;
    }

    private float CalculateMaxOrthoSizeFromBound()
    {
        if (confiner == null || confiner.BoundingShape2D == null) return float.MaxValue;
        Bounds bounds = confiner.BoundingShape2D.bounds;
        if (bounds.size.x == 0 || bounds.size.y == 0) return float.MaxValue;

        float maxH = bounds.extents.y;
        float currentAspect = (float)Screen.width / Screen.height;
        float maxW = bounds.extents.x / currentAspect;

        return Mathf.Min(maxH, maxW);
    }

    public void SetZoom(float size)
    {
        float limit = CalculateMaxOrthoSizeFromBound() - 0.05f;
        if (limit > 1000f) limit = maxSize;
        float finalMax = Mathf.Min(maxSize, limit);
        userDesiredSize = Mathf.Clamp(size, minSize, finalMax);
        currentVelocity = 0f;
        if (virtualCamera != null) virtualCamera.Lens.OrthographicSize = userDesiredSize;
    }

    public void UpdateMapBounds(Collider2D newBounds)
    {
        mapCollider = newBounds;

        if (confiner != null)
        {
            confiner.BoundingShape2D = newBounds;
            confiner.InvalidateBoundingShapeCache();
        }
    }

    public float GetCurrentZoom() => userDesiredSize;

    void OnDrawGizmos()
    {
        if (playerTransform != null && mapCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, borderThreshold);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(playerTransform.position, debugClosestPoint);
                Gizmos.DrawSphere(debugClosestPoint, 0.2f);
            }
        }
    }
}
using UnityEngine;
using Unity.Cinemachine;

public class CameraZoomController : MonoBehaviour
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

    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform playerTransform;

    private CinemachineConfiner2D confiner;
    private PolygonCollider2D polyCollider;

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
                if (confiner.BoundingShape2D is PolygonCollider2D poly)
                {
                    polyCollider = poly;
                }
            }
        }
        else
        {
            userDesiredSize = defaultSize;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void LateUpdate()
    {
        if (virtualCamera == null) return;

        float mapLimit = CalculateMaxOrthoSizeFromBound();
        float safeMaxSize = Mathf.Min(maxSize, mapLimit - 0.05f);

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

        if (enableBorderZoom && polyCollider != null && playerTransform != null)
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
        }
    }

    // Tính khoảng cách từ điểm đến cạnh gần nhất của Polygon
    private float GetDistanceToClosestBorder(Vector2 point, out Vector2 closestPointOnEdge)
    {
        float minDst = float.MaxValue;
        closestPointOnEdge = point;

        if (polyCollider == null) return float.MaxValue;

        for (int i = 0; i < polyCollider.pathCount; i++)
        {
            Vector2[] pathPoints = polyCollider.GetPath(i);

            for (int j = 0; j < pathPoints.Length; j++)
            {
                Vector2 p1 = polyCollider.transform.TransformPoint(pathPoints[j]);
                Vector2 p2 = polyCollider.transform.TransformPoint(pathPoints[(j + 1) % pathPoints.Length]);

                Vector2 closest = GetClosestPointOnSegment(point, p1, p2);
                float dst = Vector2.Distance(point, closest);

                if (dst < minDst)
                {
                    minDst = dst;
                    closestPointOnEdge = closest;
                }
            }
        }

        return minDst;
    }

    // Tìm điểm gần nhất trên đoạn thẳng AB so với điểm P
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
    public float GetCurrentZoom() => userDesiredSize;

    void OnDrawGizmos()
    {
        if (playerTransform != null && polyCollider != null)
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
using UnityEngine;

public class MousePhysicsDebugger : MonoBehaviour
{
    void Update()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Quét toàn bộ không lọc LayerMask
        Collider2D[] results = Physics2D.OverlapCircleAll(mouseWorldPos, 0.1f);

        foreach (var col in results)
        {
            Debug.Log($"<color=yellow>[DÒ TÌM]</color> Name: {col.gameObject.name} | Layer ID: {col.gameObject.layer} | Layer Name: {LayerMask.LayerToName(col.gameObject.layer)} | Trigger: {col.isTrigger}");
        }
    }

    // Vẽ một vòng tròn nhỏ màu đỏ tại chuột để bạn dễ nhìn trong thẻ Scene
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mouseWorldPos, 0.2f);
        }
    }
}
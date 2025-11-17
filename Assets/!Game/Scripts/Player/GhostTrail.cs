using UnityEngine;
using System.Collections;

public class GhostTrail : MonoBehaviour
{
    public SpriteRenderer targetRenderer; // Sprite gốc (player)
    public Material ghostMaterial;        // Material dùng để làm ghost
    public float fadeDuration = 0.5f;     // Thời gian mờ dần
    public float spawnInterval = 0.05f;   // Khoảng cách giữa các ghost
    public int ghostCount = 5;            // Số ghost tạo khi dash

    public void CreateTrail()
    {
        StartCoroutine(SpawnGhosts());
    }

    private IEnumerator SpawnGhosts()
    {
        for (int i = 0; i < ghostCount; i++)
        {
            SpawnGhost();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnGhost()
    {
        GameObject ghost = new GameObject("Ghost");
        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();

        sr.sprite = targetRenderer.sprite;
        sr.transform.position = targetRenderer.transform.position;
        sr.transform.rotation = targetRenderer.transform.rotation;
        sr.transform.localScale = targetRenderer.transform.lossyScale;
        sr.sortingLayerID = targetRenderer.sortingLayerID;
        sr.sortingOrder = targetRenderer.sortingOrder - 1;

        sr.material = ghostMaterial;
        sr.color = new Color(1f, 1f, 1f, 1f); // bắt đầu với alpha 1

        Destroy(ghost, fadeDuration); // tự xóa sau khi mờ hết

        StartCoroutine(FadeOut(sr));
    }
    private IEnumerator FadeOut(SpriteRenderer sr)
    {
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < fadeDuration)
        {
            // Check if the SpriteRenderer still exists before trying to modify its color
            if (sr != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the SpriteRenderer is still valid before final color update
        if (sr != null)
        {
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }
    }

}

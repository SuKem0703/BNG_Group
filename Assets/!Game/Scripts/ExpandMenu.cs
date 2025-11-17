using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ExpandMenu : MonoBehaviour
{
    public Button mainButton;
    public RectTransform[] subIcons;
    public float spacing = 100f;
    public float speed = 10f;

    public enum ExpandDirection { Left, Right, Up, Down }
    public ExpandDirection direction = ExpandDirection.Right;

    private bool isExpanded = false;

    void Start()
    {
        foreach (var icon in subIcons)
            icon.gameObject.SetActive(false);

        if (mainButton != null)
            mainButton.onClick.AddListener(ToggleMenu);
    }

    void ToggleMenu()
    {
        if (isExpanded)
            StartCoroutine(HideIcons());
        else
            StartCoroutine(ShowIcons());
    }

    IEnumerator ShowIcons()
    {
        for (int i = 0; i < subIcons.Length; i++)
        {
            var icon = subIcons[i];
            icon.gameObject.SetActive(true);

            Vector3 startPos = mainButton.transform.position;
            Vector3 offset = Vector3.zero;

            switch (direction)
            {
                case ExpandDirection.Right:
                    offset = new Vector3((i + 1) * spacing, 0, 0);
                    break;
                case ExpandDirection.Left:
                    offset = new Vector3(-(i + 1) * spacing, 0, 0);
                    break;
                case ExpandDirection.Up:
                    offset = new Vector3(0, (i + 1) * spacing, 0);
                    break;
                case ExpandDirection.Down:
                    offset = new Vector3(0, -(i + 1) * spacing, 0);
                    break;
            }

            Vector3 endPos = startPos + offset;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * speed;
                icon.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }

        isExpanded = true;
    }

    IEnumerator HideIcons()
    {
        for (int i = subIcons.Length - 1; i >= 0; i--)
        {
            var icon = subIcons[i];
            Vector3 startPos = icon.position;
            Vector3 endPos = mainButton.transform.position;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * speed;
                icon.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            icon.gameObject.SetActive(false);
        }

        isExpanded = false;
    }
}

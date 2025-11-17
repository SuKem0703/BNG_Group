using UnityEngine;

public class MapArrow : MonoBehaviour
{
    public enum ArrowDirection { Up, Down, Left, Right }
    public ArrowDirection arrowDirection = ArrowDirection.Up;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateArrow();
    }

    public void UpdateArrow()
    {
        switch (arrowDirection)
        {
            case ArrowDirection.Up:
                sr.sprite = Resources.Load<Sprite>("ArrowUp"); // sprite mũi lên
                sr.flipX = false;
                sr.flipY = false;
                break;
            case ArrowDirection.Down:
                sr.sprite = Resources.Load<Sprite>("ArrowUp"); // dùng mũi lên, lật dọc
                sr.flipX = false;
                sr.flipY = true;
                break;
            case ArrowDirection.Right:
                sr.sprite = Resources.Load<Sprite>("ArrowRight"); // sprite mũi phải
                sr.flipX = false;
                sr.flipY = false;
                break;
            case ArrowDirection.Left:
                sr.sprite = Resources.Load<Sprite>("ArrowRight"); // dùng mũi phải, lật ngang
                sr.flipX = true;
                sr.flipY = false;
                break;
        }
    }
}

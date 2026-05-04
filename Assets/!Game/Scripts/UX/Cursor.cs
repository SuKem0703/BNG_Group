using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public Texture2D cursorNormal;
    public Texture2D cursorClick;
    public Texture2D cursorBattle;
    public Vector2 hotSpot = Vector2.zero;

    void Start()
    {
        Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
    }

    void Update()
    {
        if (PlayerStats.IsOnBattle)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.SetCursor(cursorBattle, hotSpot, CursorMode.Auto);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
            }
            return;
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.SetCursor(cursorClick, hotSpot, CursorMode.Auto);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
            }
        }
    }
}
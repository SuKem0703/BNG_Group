using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public static CustomCursor Instance { get; private set; }

    public Texture2D cursorNormal;
    public Texture2D cursorClick;
    public Texture2D cursorBattle;
    public Vector2 hotSpot = Vector2.zero;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
    }

    void Update()
    {
        bool isOnBattle = false;
        if (PlayerStats.Instance != null)
        {
            isOnBattle = PlayerStats.Instance.netIsOnBattle.Value;
        }

        if (isOnBattle)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.SetCursor(cursorBattle, hotSpot, CursorMode.Auto);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
            }
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
using TMPro;
using UnityEngine;

public class AddOutline : MonoBehaviour
{
    public float outlineWidth;
    void Start()
    {
        var tmp = GetComponent<TextMeshProUGUI>();
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = outlineWidth;
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputFieldTabEnter : MonoBehaviour
{
    public TMP_InputField[] fields; // Các ô nhập
    public Button submitButton; // Button để kích hoạt khi Enter

    void Update()
    {
        // Tab → chuyển sang ô kế tiếp
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TMP_InputField current = EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();
            if (current != null)
            {
                current.OnSubmit(null);

                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] == current)
                    {
                        int next = (i + 1) % fields.Length;
                        EventSystem.current.SetSelectedGameObject(fields[next].gameObject);
                        break;
                    }
                }
            }
        }

        // Enter → kích hoạt button
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (submitButton != null)
            {
                submitButton.onClick.Invoke();
            }
        }
    }
}

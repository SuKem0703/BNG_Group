using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputAutoAssign : MonoBehaviour
{
    void Start()
    {
        var playerInput = GetComponent<PlayerInput>();

        if (playerInput.camera == null)
        {
            var cam = Camera.main;
            if (cam != null) playerInput.camera = cam;
        }

        if (playerInput.uiInputModule == null)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (uiModule != null) playerInput.uiInputModule = uiModule;
            }
        }
    }
}
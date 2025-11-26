using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine; // Unity 6

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

        GameObject camObj = GameObject.FindGameObjectWithTag("PlayerCamera");

        if (camObj != null)
        {
            var cineCam = camObj.GetComponent<CinemachineCamera>();
            if (cineCam != null)
            {
                cineCam.Follow = transform;
            }
        }
        else
        {
            var fallbackCam = GameObject.Find("Player Camera");
            if (fallbackCam != null)
            {
                var cm = fallbackCam.GetComponent<CinemachineCamera>();
                if (cm != null) cm.Follow = transform;
            }
        }
    }
}
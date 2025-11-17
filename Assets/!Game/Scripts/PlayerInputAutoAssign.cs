using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputAutoAssign : MonoBehaviour
{
    void Start()
    {
        var playerInput = GetComponent<PlayerInput>();

        // Gán lại camera nếu null
        if (playerInput.camera == null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                playerInput.camera = cam;
            }
        }

        // Gán lại UI Input Module nếu null
        if (playerInput.uiInputModule == null)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (uiModule != null)
                {
                    playerInput.uiInputModule = uiModule;
                }
            }
        }

        // Gán transform của player.pref vào Tracking Target của CinemachineCamera
        var cineCam = FindFirstObjectByType<CinemachineCamera>();
        if (cineCam != null)
        {
            cineCam.Follow = transform;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy CinemachineVirtualCamera để gán Tracking Target.");
        }
    }
}

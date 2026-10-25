using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Homeward.Hopscotch
{
    public sealed class TapInputReader : MonoBehaviour
    {
        public event Action Tapped;

        private void Update()
        {
            if (WasPressedThisFrame())
            {
                Tapped?.Invoke();
            }
        }

        private static bool WasPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                return true;
            }

            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}

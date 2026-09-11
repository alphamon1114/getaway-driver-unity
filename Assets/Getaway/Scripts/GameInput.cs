using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Getaway
{
    // Works with either input backend when these scripts are imported elsewhere.
    public static class GameInput
    {
        public static float Throttle
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                return k == null ? 0 : ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1 : 0) - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1 : 0);
#else
                return (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
#endif
            }
        }
        public static float Steer
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                return k == null ? 0 : ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1 : 0) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1 : 0);
#else
                return (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
#endif
            }
        }
        public static bool Brake
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
                return Input.GetKey(KeyCode.Space);
#endif
            }
        }
        public static bool PausePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }
        public static bool ResetPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }
    }
}

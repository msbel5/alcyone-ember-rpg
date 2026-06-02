using UnityEngine;
using EmberCrpg.Domain.Configuration;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EmberCrpg.Presentation.Ember.Inputs
{
    internal static class EmberInputHardware
    {
        private static Vector2 _smoothedLook;

        public static Vector2 Move
        {
            get
            {
                var x = AxisRaw("Horizontal");
                var y = AxisRaw("Vertical");
                return new Vector2(x, y);
            }
        }

        public static Vector2 Look => Mouse.current?.delta.ReadValue() ?? Vector2.zero;

        public static Vector2 LookSmoothed
        {
            get
            {
                var alpha = EmberRuntimeOptionsProvider.Current.Input.LookSmoothingAlpha;
                _smoothedLook = Vector2.Lerp(_smoothedLook, Look, alpha);
                return _smoothedLook;
            }
        }

        public static bool KeyDown(KeyCode key) => ControlFor(key)?.wasPressedThisFrame == true;

        public static bool Key(KeyCode key) => ControlFor(key)?.isPressed == true;

        public static bool MouseDown(int button)
        {
            var mouse = Mouse.current;
            if (mouse == null) return false;
            return button switch
            {
                0 => mouse.leftButton.wasPressedThisFrame,
                1 => mouse.rightButton.wasPressedThisFrame,
                2 => mouse.middleButton.wasPressedThisFrame,
                _ => false
            };
        }

        public static float AxisRaw(string axisName)
        {
            return axisName switch
            {
                "Horizontal" => DigitalAxis(KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow),
                "Vertical" => DigitalAxis(KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow),
                "Mouse X" => Look.x,
                "Mouse Y" => Look.y,
                _ => 0f
            };
        }

        public static float Axis(string axisName)
        {
            return axisName switch
            {
                "Mouse X" => LookSmoothed.x,
                "Mouse Y" => LookSmoothed.y,
                _ => AxisRaw(axisName)
            };
        }

#if UNITY_INCLUDE_TESTS
        public static void ResetForTests()
        {
            _smoothedLook = Vector2.zero;
        }
#endif

        private static float DigitalAxis(KeyCode negative, KeyCode positive, KeyCode altNegative, KeyCode altPositive)
        {
            var value = 0f;
            if (Key(negative) || Key(altNegative)) value -= 1f;
            if (Key(positive) || Key(altPositive)) value += 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }

        private static KeyControl ControlFor(KeyCode code)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return null;
            var key = ToInputSystemKey(code);
            return key == UnityEngine.InputSystem.Key.None ? null : keyboard[key];
        }

        private static UnityEngine.InputSystem.Key ToInputSystemKey(KeyCode code)
        {
            if (code >= KeyCode.A && code <= KeyCode.Z)
                return (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.A + ((int)code - (int)KeyCode.A));
            if (code >= KeyCode.F1 && code <= KeyCode.F12)
                return (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.F1 + ((int)code - (int)KeyCode.F1));

            return code switch
            {
                KeyCode.Alpha0 => UnityEngine.InputSystem.Key.Digit0,
                KeyCode.Alpha1 => UnityEngine.InputSystem.Key.Digit1,
                KeyCode.Alpha2 => UnityEngine.InputSystem.Key.Digit2,
                KeyCode.Alpha3 => UnityEngine.InputSystem.Key.Digit3,
                KeyCode.Alpha4 => UnityEngine.InputSystem.Key.Digit4,
                KeyCode.Alpha5 => UnityEngine.InputSystem.Key.Digit5,
                KeyCode.Alpha6 => UnityEngine.InputSystem.Key.Digit6,
                KeyCode.Alpha7 => UnityEngine.InputSystem.Key.Digit7,
                KeyCode.Alpha8 => UnityEngine.InputSystem.Key.Digit8,
                KeyCode.Alpha9 => UnityEngine.InputSystem.Key.Digit9,
                KeyCode.Space => UnityEngine.InputSystem.Key.Space,
                KeyCode.Escape => UnityEngine.InputSystem.Key.Escape,
                KeyCode.Tab => UnityEngine.InputSystem.Key.Tab,
                KeyCode.LeftShift => UnityEngine.InputSystem.Key.LeftShift,
                KeyCode.RightShift => UnityEngine.InputSystem.Key.RightShift,
                KeyCode.LeftArrow => UnityEngine.InputSystem.Key.LeftArrow,
                KeyCode.RightArrow => UnityEngine.InputSystem.Key.RightArrow,
                KeyCode.UpArrow => UnityEngine.InputSystem.Key.UpArrow,
                KeyCode.DownArrow => UnityEngine.InputSystem.Key.DownArrow,
                _ => UnityEngine.InputSystem.Key.None
            };
        }
    }
}

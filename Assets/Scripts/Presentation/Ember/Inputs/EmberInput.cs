using UnityEngine;
using EmberCrpg.Domain.Configuration;

namespace EmberCrpg.Presentation.Ember.Inputs
{
    /// <summary>
    /// Single semantic input facade for gameplay/UI. The public API stays stable while the body is
    /// backed by com.unity.inputsystem actions and device controls.
    /// </summary>
    public static class EmberInput
    {
        private static EmberInputActions _actions;
        private static Vector2 _smoothedLook;

        public static Vector2 Move => Actions.Move.ReadValue<Vector2>();
        public static Vector2 Look => Actions.Look.ReadValue<Vector2>();
        public static Vector2 LookSmoothed
        {
            get
            {
                var alpha = EmberRuntimeOptionsProvider.Current.Input.LookSmoothingAlpha;
                _smoothedLook = Vector2.Lerp(_smoothedLook, Look, alpha);
                return _smoothedLook;
            }
        }

        public static bool Sprint => Actions.Sprint.IsPressed();
        public static bool JumpDown => Actions.Jump.WasPressedThisFrame();
        public static bool JumpKeyDown => JumpDown;

        public static bool Interact => Actions.Interact.WasPressedThisFrame();
        public static bool ToggleCursor => Actions.ToggleCursor.WasPressedThisFrame();
        public static bool RegenWorld => Actions.RegenWorld.WasPressedThisFrame();
        public static bool ToggleMap => Actions.ToggleMap.WasPressedThisFrame();

        public static bool SaveQuick => Actions.SaveQuick.WasPressedThisFrame();
        public static bool LoadQuick => Actions.LoadQuick.WasPressedThisFrame();

        public static bool PauseDown => Actions.Pause.WasPressedThisFrame();
        public static bool PauseHeld => Actions.Pause.IsPressed();

        public static bool AttackClick => Actions.Attack.WasPressedThisFrame();
        public static bool SecondaryClick => Actions.Secondary.WasPressedThisFrame();
        public static bool MeleeSwing => Actions.MeleeSwing.WasPressedThisFrame();

        public static int NumberKeyDown()
        {
            var slots = EmberRuntimeOptionsProvider.Current.Input.NumberSlots;
            for (int i = 0; i < slots; i++)
                if (NumberKeyDown(i + 1)) return i + 1;
            return 0;
        }

        public static bool NumberKeyDown(int oneBased)
            => oneBased >= 1 && oneBased <= 9 && EmberInputHardware.KeyDown(KeyCode.Alpha1 + (oneBased - 1));

        public static int FunctionKeyDown()
        {
            var slots = EmberRuntimeOptionsProvider.Current.Input.FunctionSlots;
            for (int i = 0; i < slots; i++)
                if (EmberInputHardware.KeyDown(KeyCode.F1 + i)) return i + 1;
            return 0;
        }

        public static bool KeyDown(KeyCode key) => EmberInputHardware.KeyDown(key);
        public static bool Key(KeyCode key) => EmberInputHardware.Key(key);
        public static bool MouseDown(int button) => EmberInputHardware.MouseDown(button);
        public static float AxisRaw(string axisName)
        {
            return axisName switch
            {
                "Horizontal" => Move.x,
                "Vertical" => Move.y,
                "Mouse X" => Look.x,
                "Mouse Y" => Look.y,
                _ => EmberInputHardware.AxisRaw(axisName)
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
            _actions?.Dispose();
            _actions = null;
            _smoothedLook = Vector2.zero;
            EmberInputHardware.ResetForTests();
        }

        public static void EnableForTests()
        {
            _ = Actions;
        }
#endif

        private static EmberInputActions Actions
        {
            get
            {
                _actions ??= new EmberInputActions();
                _actions.Enable();
                return _actions;
            }
        }
    }
}

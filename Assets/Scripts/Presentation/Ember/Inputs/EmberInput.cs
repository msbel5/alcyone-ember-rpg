using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Inputs
{
    /// <summary>
    /// Single semantic input facade for gameplay/UI. The public API stays stable while the body is
    /// backed by com.unity.inputsystem actions and device controls.
    /// </summary>
    public static class EmberInput
    {
        private static EmberInputActions _actions;

        public static Vector2 Move => EmberInputHardware.Move;
        public static Vector2 Look => EmberInputHardware.Look;
        public static Vector2 LookSmoothed => EmberInputHardware.LookSmoothed;

        public static bool Sprint => Actions.Sprint.IsPressed();
        public static bool JumpDown => Actions.Jump.WasPressedThisFrame();
        public static bool JumpKeyDown => EmberInputHardware.KeyDown(KeyCode.Space);

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
            for (int i = 0; i < 9; i++)
                if (NumberKeyDown(i + 1)) return i + 1;
            return 0;
        }

        public static bool NumberKeyDown(int oneBased)
            => oneBased >= 1 && oneBased <= 9 && EmberInputHardware.KeyDown(KeyCode.Alpha1 + (oneBased - 1));

        public static int FunctionKeyDown()
        {
            for (int i = 0; i < 12; i++)
                if (EmberInputHardware.KeyDown(KeyCode.F1 + i)) return i + 1;
            return 0;
        }

        public static bool KeyDown(KeyCode key) => EmberInputHardware.KeyDown(key);
        public static bool Key(KeyCode key) => EmberInputHardware.Key(key);
        public static bool MouseDown(int button) => EmberInputHardware.MouseDown(button);
        public static float AxisRaw(string axisName) => EmberInputHardware.AxisRaw(axisName);
        public static float Axis(string axisName) => EmberInputHardware.Axis(axisName);

#if UNITY_INCLUDE_TESTS
        public static void ResetForTests()
        {
            _actions?.Dispose();
            _actions = null;
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

using System;
using EmberCrpg.Domain.Configuration;
using UnityEngine.InputSystem;

namespace EmberCrpg.Presentation.Ember.Inputs
{
    internal sealed class EmberInputActions : IDisposable
    {
        private readonly InputActionMap _map = new("Gameplay");

        public EmberInputActions()
        {
            var options = EmberRuntimeOptionsProvider.Current.Input;
            Move = Move2D("Move", options);
            Look = Vector2Value("Look", options.LookPath);
            Jump = Button("Jump", options.JumpPath);
            Sprint = Button("Sprint", options.SprintPath);
            Interact = Button("Interact", options.InteractPath);
            ToggleCursor = Button("ToggleCursor", options.ToggleCursorPath);
            RegenWorld = Button("RegenWorld", options.RegenWorldPath);
            ToggleMap = Button("ToggleMap", options.ToggleMapPath);
            ToggleColonyPanels = Button("ToggleColonyPanels", options.ToggleColonyPath);
            SaveQuick = Button("SaveQuick", options.SaveQuickPath);
            LoadQuick = Button("LoadQuick", options.LoadQuickPath);
            Pause = Button("Pause", options.PausePath);
            Attack = Button("Attack", options.AttackPath);
            Secondary = Button("Secondary", options.SecondaryPath);
            MeleeSwing = Button("MeleeSwing", options.MeleeSwingPath);
        }

        public InputAction Move { get; }
        public InputAction Look { get; }
        public InputAction Jump { get; }
        public InputAction Sprint { get; }
        public InputAction Interact { get; }
        public InputAction ToggleCursor { get; }
        public InputAction RegenWorld { get; }
        public InputAction ToggleMap { get; }
        public InputAction ToggleColonyPanels { get; }
        public InputAction SaveQuick { get; }
        public InputAction LoadQuick { get; }
        public InputAction Pause { get; }
        public InputAction Attack { get; }
        public InputAction Secondary { get; }
        public InputAction MeleeSwing { get; }

        public void Enable()
        {
            if (!_map.enabled) _map.Enable();
        }

        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }

        private InputAction Button(string name, string path)
        {
            var action = _map.AddAction(name, InputActionType.Button, path);
            action.expectedControlType = "Button";
            return action;
        }

        private InputAction Vector2Value(string name, string path)
        {
            var action = _map.AddAction(name, InputActionType.Value, path);
            action.expectedControlType = "Vector2";
            return action;
        }

        private InputAction Move2D(string name, InputRuntimeOptions options)
        {
            var action = _map.AddAction(name, InputActionType.Value);
            action.expectedControlType = "Vector2";

            AddMoveComposite(
                action,
                options.MoveUpPath,
                options.MoveDownPath,
                options.MoveLeftPath,
                options.MoveRightPath);

            if (!string.IsNullOrWhiteSpace(options.MoveUpAltPath)
                && !string.IsNullOrWhiteSpace(options.MoveDownAltPath)
                && !string.IsNullOrWhiteSpace(options.MoveLeftAltPath)
                && !string.IsNullOrWhiteSpace(options.MoveRightAltPath))
            {
                AddMoveComposite(
                    action,
                    options.MoveUpAltPath,
                    options.MoveDownAltPath,
                    options.MoveLeftAltPath,
                    options.MoveRightAltPath);
            }

            return action;
        }

        private static void AddMoveComposite(
            InputAction action,
            string up,
            string down,
            string left,
            string right)
        {
            var composite = action.AddCompositeBinding("2DVector(mode=1)");
            composite.With("Up", up);
            composite.With("Down", down);
            composite.With("Left", left);
            composite.With("Right", right);
        }
    }
}

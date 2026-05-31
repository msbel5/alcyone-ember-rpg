using System;
using UnityEngine.InputSystem;

namespace EmberCrpg.Presentation.Ember.Inputs
{
    internal sealed class EmberInputActions : IDisposable
    {
        private readonly InputActionMap _map = new("Gameplay");

        public EmberInputActions()
        {
            Jump = Button("Jump", "<Keyboard>/space");
            Sprint = Button("Sprint", "<Keyboard>/leftShift");
            Interact = Button("Interact", "<Keyboard>/e");
            ToggleCursor = Button("ToggleCursor", "<Keyboard>/f1");
            RegenWorld = Button("RegenWorld", "<Keyboard>/r");
            ToggleMap = Button("ToggleMap", "<Keyboard>/tab");
            SaveQuick = Button("SaveQuick", "<Keyboard>/f5");
            LoadQuick = Button("LoadQuick", "<Keyboard>/f9");
            Pause = Button("Pause", "<Keyboard>/escape");
            Attack = Button("Attack", "<Mouse>/leftButton");
            Secondary = Button("Secondary", "<Mouse>/rightButton");
            MeleeSwing = Button("MeleeSwing", "<Keyboard>/f");
        }

        public InputAction Jump { get; }
        public InputAction Sprint { get; }
        public InputAction Interact { get; }
        public InputAction ToggleCursor { get; }
        public InputAction RegenWorld { get; }
        public InputAction ToggleMap { get; }
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
    }
}

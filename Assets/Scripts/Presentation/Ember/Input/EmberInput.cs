using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Inputs
{
    /// <summary>
    /// EMB-015: the single semantic input facade for active Ember runtime code. Every gameplay/UI
    /// script reads input through this type instead of touching <see cref="UnityEngine.Input"/>
    /// directly, so the project has ONE choke point for the legacy Input Manager today and can swap
    /// the body for com.unity.inputsystem later without editing 30+ call sites.
    ///
    /// Members come in two flavours:
    ///  - Semantic actions (Move, Look, Interact, SaveQuick, AttackClick, ...) name the intent, not the
    ///    key — these are the preferred call sites.
    ///  - Thin passthroughs (KeyDown/Key/MouseDown/AxisRaw/Axis) exist only for sites that bind a
    ///    *configurable* KeyCode/axis at the inspector (combat dodge/cast/pause keys, camera toggle):
    ///    they keep that configurability while still routing through the one facade so the static-audit
    ///    "direct UnityEngine.Input." count in active runtime stays at ~0.
    ///
    /// Out of scope by design: the legacy Slice* controllers (EMB-057) still use UnityEngine.Input
    /// directly — they are being retired, not migrated.
    /// </summary>
    public static class EmberInput
    {
        // ----- Movement / look -------------------------------------------------------------------
        /// <summary>WASD / arrows as (x=strafe, y=forward), raw (no smoothing) for deterministic feel.</summary>
        public static Vector2 Move => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        /// <summary>Raw mouse delta (x=yaw, y=pitch). Use for first-person look that does its own smoothing.</summary>
        public static Vector2 Look => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        /// <summary>Unity-smoothed mouse delta (x=yaw, y=pitch). Use for orbit/third-person rigs.</summary>
        public static Vector2 LookSmoothed => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        public static bool Sprint => Input.GetKey(KeyCode.LeftShift);

        /// <summary>Jump pressed this frame, accepting either the "Jump" button or Space.</summary>
        public static bool JumpDown => Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);

        /// <summary>Space pressed this frame (rigs that want Space specifically, not the Jump button).</summary>
        public static bool JumpKeyDown => Input.GetKeyDown(KeyCode.Space);

        // ----- Interaction / world ---------------------------------------------------------------
        public static bool Interact => Input.GetKeyDown(KeyCode.E);
        public static bool ToggleCursor => Input.GetKeyDown(KeyCode.F1);
        public static bool RegenWorld => Input.GetKeyDown(KeyCode.R);
        public static bool ToggleMap => Input.GetKeyDown(KeyCode.Tab);

        // ----- Save ------------------------------------------------------------------------------
        public static bool SaveQuick => Input.GetKeyDown(KeyCode.F5);
        public static bool LoadQuick => Input.GetKeyDown(KeyCode.F9);

        // ----- Menu / cancel ---------------------------------------------------------------------
        /// <summary>Escape pressed this frame (open/close pause, cancel dialog).</summary>
        public static bool PauseDown => Input.GetKeyDown(KeyCode.Escape);
        /// <summary>Escape held (rigs that gate on holding cancel).</summary>
        public static bool PauseHeld => Input.GetKey(KeyCode.Escape);

        // ----- Combat ----------------------------------------------------------------------------
        public static bool AttackClick => Input.GetMouseButtonDown(0);
        public static bool SecondaryClick => Input.GetMouseButtonDown(1);
        public static bool MeleeSwing => Input.GetKeyDown(KeyCode.F);

        // ----- Number row 1..9 (dialog topics / spell slots / hotbar) ----------------------------
        /// <summary>Returns the 1..9 number-row key pressed this frame, or 0 if none. One scan replaces
        /// per-site <c>GetKeyDown(KeyCode.Alpha1 + i)</c> loops (dialog topics, spell slots, hotbar).</summary>
        public static int NumberKeyDown()
        {
            for (int i = 0; i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return i + 1;
            return 0;
        }

        /// <summary>True if the given 1..9 number-row key was pressed this frame.</summary>
        public static bool NumberKeyDown(int oneBased)
            => oneBased >= 1 && oneBased <= 9 && Input.GetKeyDown(KeyCode.Alpha1 + (oneBased - 1));

        // ----- Thin passthroughs (configurable bindings only) ------------------------------------
        // These exist solely so inspector-bound KeyCode/axis fields still funnel through this facade.
        // Prefer a semantic member above when the binding is fixed.
        public static bool KeyDown(KeyCode key) => Input.GetKeyDown(key);
        public static bool Key(KeyCode key) => Input.GetKey(key);
        public static bool MouseDown(int button) => Input.GetMouseButtonDown(button);
        public static float AxisRaw(string axisName) => Input.GetAxisRaw(axisName);
        public static float Axis(string axisName) => Input.GetAxis(axisName);
    }
}

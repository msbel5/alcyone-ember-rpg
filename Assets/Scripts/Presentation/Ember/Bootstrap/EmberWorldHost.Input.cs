using EmberCrpg.Domain.Configuration;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.Runtime;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.UI.InGame;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        private void Update()
        {
            // The redesigned PauseView owns Esc now (InGameUiController routes Esc → PauseView / close); the
            // legacy Esc-hold-to-quit must yield so the two don't both react. OwnsInput is true once mounted.
            if (!InGameUiController.OwnsInput) HandleQuitInput();

            if (EmberInput.RegenWorld && !InGameUiController.OwnsInput)
            {
                // The Oracle takes over the dialog: END any NPC conversation first so its topics + replies
                // can't bleed into the Oracle's. This was the reported bug — open the Oracle after an NPC chat,
                // pick a topic, and the previous NPC answered too (the host proxy still forwarded to it, and its
                // in-flight async reply landed in the Oracle box). EndConversation also bumps the conversation
                // serial, so that pending reply is discarded.
                (_adapter as IDialogSource)?.EndConversation();
                EmberCrpg.Presentation.Ember.Audio.SpeechDirector.StopConversationSpeech();
                // Immediate placeholder line ("The oracle consults the fates…"); the real LLM prophecy
                // resolves async and is swapped in below via TryConsumeResolvedFate (BUG-4).
                _fateLine = _oracle.ConsultFate();
                _fateTimer = EmberRuntimeOptionsProvider.Current.WorldHost.FatePlaceholderSeconds;
                RouteFateToDialog(_fateLine);
            }

            // BUG-4: poll for the resolved oracle prophecy (LLM-flavoured, or the deterministic fate
            // bucket as a floor). When it lands a frame+ later, replace the placeholder in the dialog
            // and extend the dwell so the player can actually read it — previously this only hit the log.
            if (!InGameUiController.OwnsInput)
            {
                var resolvedFate = _oracle.TryConsumeResolvedFate();
                if (!string.IsNullOrEmpty(resolvedFate))
                {
                    _fateLine = resolvedFate;
                    _fateTimer = EmberRuntimeOptionsProvider.Current.WorldHost.FateResolvedSeconds;
                    RouteFateToDialog(_fateLine);
                }
            }

            // BUG-2: toggle the standing colony overlay (JobQueue / Faction / ColonyNeeds). Hidden by
            // default so action scenes aren't cluttered; the player opens it on demand.
            if (EmberInput.ToggleColonyPanels && !InGameUiController.OwnsInput)
                SetColonyPanelsVisible(!_colonyPanelsVisible);

            // M: open/close the overland world map. The generated overland is otherwise invisible — this
            // makes the 409,600 km² world (biomes + settlements + the player's home region) legible. Paint
            // on open; the map is static within a session so it needs no per-frame refresh.
            if (EmberInput.KeyDown(KeyCode.M) && !InGameUiController.OwnsInput)
            {
                _overlandMapPanel?.Toggle();
                if (_overlandMapPanel != null && _overlandMapPanel.IsVisible)
                    _overlandMapPanel.Render(_worldView.Overland, _worldView.PlayerOverlandTile, _worldView.StartingSettlementName);
            }

            _fateTimer = WorldHostInputPolicy.StepFateTimer(_fateTimer, Time.deltaTime, () => _fateLine = string.Empty);

            if (EmberInput.ToggleInventory && !InGameUiController.OwnsInput)
            {
                // Codex audit (sixth pass D-P3 #D1): if the scene wires an
                // EmberPlayerInventoryToggle (every Phase* scene does, plus the
                // PlayerRig builder), delegate to its Toggle() so the toggle
                // component is no longer dead code. Falls back to the inline
                // loop for any scene that omits the component.
                var toggle = Object.FindFirstObjectByType<EmberPlayerInventoryToggle>(FindObjectsInactive.Include);
                if (toggle != null)
                {
                    toggle.Toggle();
                }
                else if (_inventoryGrids != null)
                foreach (var inv in _inventoryGrids)
                {
                    bool active = !inv.gameObject.activeSelf;
                    inv.gameObject.SetActive(active);

                    // If opening inventory, unlock cursor. If closing, lock it (if not in dialog)
                    if (active)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        // Simple check: only lock if no dialog is visible
                        bool dialogVisible = false;
                        foreach (var d in Object.FindObjectsByType<DialogBoxPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        {
                            dialogVisible = true;
                            break;
                        }

                        if (!dialogVisible)
                        {
                            Cursor.lockState = CursorLockMode.Locked;
                            Cursor.visible = false;
                        }
                    }
                }
            }

            // Codex audit (sixth pass A-P1 #8): bail out of Alpha1..5 spell
            // selection while a dialog panel is open — those keys belong to
            // the dialog topic chooser. Without this short-circuit, a single
            // "1" press fires SelectTopic(topics[0]) AND mutates
            // _selectedSpellSlot AND queues a spell cast on the next swing.
            _selectedSpellSlot = WorldHostInputPolicy.ResolveSelectedSpellSlot(
                IsModalOpen(),
                _selectedSpellSlot,
                EmberRuntimeOptionsProvider.Current.WorldHost.SpellSlotCount,
                EmberInput.NumberKeyDown);
        }

        /// <summary>
        /// Codex audit (sixth pass A-P1 #8): central modal predicate so
        /// non-dialog input handlers can yield to the dialog panel. Cheap —
        /// FindFirstObjectByType is O(scene size) per call, but only runs
        /// once per Update tick.
        /// </summary>
        internal static bool IsModalOpen()
        {
            // The new in-game UI (InGameUiController) is the canonical modal owner now: when any of its
            // 16 screens or the ☰ browser is open it pauses the world + frees the cursor, so FPS look/move
            // and the interact raycaster must yield to it exactly as they do for the legacy panels.
            return WorldHostInputPolicy.IsModalOpen() || InGameUiController.AnyScreenOpen
                || InGameUiController.TypingFocused; // typing swallows gameplay keys everywhere
        }

        private void HandleQuitInput()
        {
            _escHoldTimer = WorldHostInputPolicy.StepEscapeHoldTimer(
                _escHoldTimer,
                IsModalOpen(),
                Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include) != null,
                EmberInput.PauseDown,
                EmberInput.PauseHeld,
                Time.unscaledDeltaTime,
                EmberRuntimeOptionsProvider.Current.WorldHost.EscapeHoldQuitSeconds,
                QuitApplication);
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

    }
}

using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Inventory panel toggle helper. Codex audit Batch 3 / Finding D-1:
    /// previously this component listened for Tab directly, which collided with
    /// the same key handler in <c>EmberWorldHost.Update</c> — both ran every
    /// frame, so Tab cycled the inventory state twice per press (net zero on
    /// the panel but flipping cursor lock back and forth). Input ownership now
    /// lives exclusively in EmberWorldHost; this class exists as a public
    /// <see cref="Toggle"/> entry point that EmberWorldHost (or a future input
    /// router) can call. Scenes that still carry the legacy component will run
    /// it as a no-op MonoBehaviour without input subscription.
    /// </summary>
    public sealed class EmberPlayerInventoryToggle : MonoBehaviour
    {
        private InventoryGrid _inventory;

        /// <summary>
        /// External entry point — invoked by the central input router. The
        /// previous Tab-listening <c>Update</c> was removed to ensure single
        /// ownership of the Tab keystroke.
        /// </summary>
        public void Toggle()
        {
            if (_inventory == null)
            {
                _inventory = FindFirstObjectByType<InventoryGrid>(FindObjectsInactive.Include);
            }

            if (_inventory != null)
            {
                bool nextState = !_inventory.gameObject.activeSelf;
                _inventory.gameObject.SetActive(nextState);

                if (nextState)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    // Only re-lock if no other UI is open (like dialog)
                    var dialog = FindFirstObjectByType<DialogBoxPanel>(FindObjectsInactive.Include);
                    if (dialog == null || !dialog.gameObject.activeInHierarchy)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }
        }
    }
}

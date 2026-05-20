using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Listens for the Tab key to toggle the inventory panel.
    /// </summary>
    public sealed class EmberPlayerInventoryToggle : MonoBehaviour
    {
        private InventoryGrid _inventory;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Toggle();
            }
        }

        private void Toggle()
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

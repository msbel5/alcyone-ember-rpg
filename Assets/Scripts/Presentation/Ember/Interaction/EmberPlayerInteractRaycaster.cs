using UnityEngine;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Adapters;

namespace EmberCrpg.Presentation.Ember.Interaction
{
    /// <summary>
    /// Raycasts forward from the player's eye camera to detect interactables.
    /// </summary>
    public sealed class EmberPlayerInteractRaycaster : MonoBehaviour
    {
        [SerializeField] private float _interactDistance = 3.5f;

        private Transform _eye;
        private DialogBoxPanel _dialogPanel;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera");
            // Dialog panel is typically found in the scene's UI canvas
        }

        private void Update()
        {
            if (_eye == null) return;

            Ray ray = new Ray(_eye.position, _eye.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance))
            {
                var interactable = hit.collider.GetComponentInParent<EmberInteractable>();
                var portal = hit.collider.GetComponentInParent<EmberScenePortal>();

                if (interactable != null)
                {
                    // For now, we assume [E] is the interaction key
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        OpenDialog(interactable);
                    }
                }
                else if (portal != null)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        portal.Activate();
                    }
                }
            }
        }

        private void OpenDialog(EmberInteractable target)
        {
            if (_dialogPanel == null)
            {
                _dialogPanel = FindFirstObjectByType<DialogBoxPanel>(FindObjectsInactive.Include);
            }

            if (_dialogPanel != null)
            {
                var adapter = EmberDomainAdapterLocator.Current;
                if (adapter != null)
                {
                    // Codex audit (fourth pass D-P2): previously bypassed
                    // IPlayerCommandSink.TryInteract entirely and reached
                    // straight into GetDialogSource. Route through TryInteract
                    // so the adapter can apply domain side-effects (memory
                    // marker, dialog-seen counter); then bind the dialog
                    // panel to the returned source. The placeholder adapter's
                    // default TryInteract is a no-op-success so legacy scenes
                    // still get a panel even without a real domain backing.
                    adapter.TryInteract(target.DisplayName);
                    _dialogPanel.Source = adapter.GetDialogSource(target.DisplayName);
                    _dialogPanel.gameObject.SetActive(true);

                    // Unlock cursor when dialog is open
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
    }
}

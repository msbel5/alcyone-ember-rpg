using UnityEngine;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.Interaction
{
    /// <summary>
    /// AAA Polished raycaster. Triggers Dialog and manages camera focus/damping/DOF.
    /// </summary>
    public sealed class EmberPlayerInteractRaycaster : MonoBehaviour
    {
        [SerializeField] private float _interactDistance = 5.0f;

        private Transform _eye;
        private DialogBoxPanel _dialogPanel;
        private CinemachineCamera _vcam;
        private DepthOfField _dof;

        private void Awake()
        {
            _eye = transform.Find("EyeCamera") ?? GetComponentInChildren<UnityEngine.Camera>()?.transform;
            _vcam = GetComponentInChildren<CinemachineCamera>(includeInactive: true);
            
            gameObject.layer = 2;
            foreach (Transform t in transform) t.gameObject.layer = 2;

            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var v in volumes)
            {
                if (v.isGlobal && v.sharedProfile != null && v.sharedProfile.TryGet<DepthOfField>(out var dof))
                {
                    _dof = dof;
                    break;
                }
            }
        }

        private void Update()
        {
            if (_eye == null) return;
            if (EmberCrpg.Presentation.Ember.Bootstrap.EmberWorldHost.IsModalOpen())
            {
                UpdateDofFocus();
                return;
            }
            else
            {
                if (_dof != null) _dof.active = false;
            }

            Ray ray = new Ray(_eye.position, _eye.forward);
            int mask = ~(1 << 2);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, mask))
            {
                var interactable = hit.collider.GetComponentInParent<EmberInteractable>();
                var portal = hit.collider.GetComponentInParent<EmberScenePortal>();

                if (interactable != null)
                {
                    if (EmberInput.Interact)
                    {
                        OpenDialog(interactable, hit.point);
                    }
                }
                else if (portal != null)
                {
                    if (EmberInput.Interact)
                    {
                        portal.Activate();
                    }
                }
            }
        }

        private void UpdateDofFocus()
        {
            if (_dof == null) return;
            _dof.active = true;
            // Simple focus on a fixed distance or raycast hit
            _dof.focusDistance.Override(2.0f); 
        }

        private void OpenDialog(EmberInteractable target, Vector3 hitPoint)
        {
            if (_dialogPanel == null)
                _dialogPanel = FindFirstObjectByType<DialogBoxPanel>(FindObjectsInactive.Include);

            if (_dialogPanel != null)
            {
                var commands = EmberDomainAdapterLocator.PlayerCommandSink;
                if (commands != null)
                {
                    // DLG-01: prefer the STABLE-id resolution path when the interactable carries an
                    // actor id (set per-actor in the scene or by a runtime spawner). Only fall back to
                    // the brittle display-name lookup for legacy interactables with no id authored.
                    if (target.HasActorId) commands.TryInteract(target.ActorId);
                    else commands.TryInteract(target.DisplayName);
                    _dialogPanel.Source = target.HasActorId
                        ? commands.GetDialogSource(target.ActorId)
                        : commands.GetDialogSource(target.DisplayName);
                    _dialogPanel.gameObject.SetActive(true);

                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    
                    if (_dof != null)
                    {
                        float dist = Vector3.Distance(_eye.position, hitPoint);
                        _dof.focusDistance.Override(dist);
                        _dof.active = true;
                    }
                }
            }
        }
    }
}

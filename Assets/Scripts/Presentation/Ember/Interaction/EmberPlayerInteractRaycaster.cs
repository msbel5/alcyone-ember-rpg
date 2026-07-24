using UnityEngine;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using EmberCrpg.Presentation.Ember.Inputs;
using EmberCrpg.Presentation.Ember.UI.InGame;

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
            var options = EmberRuntimeOptionsProvider.Current.Interaction;
            _interactDistance = options.InteractDistance;
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
                        return;
                    }
                }
                else if (portal != null)
                {
                    if (EmberInput.Interact)
                    {
                        portal.Activate();
                        return;
                    }
                }
            }

            // BUYER FIX (Daggerfall rule): a pixel-perfect crosshair is not the price of a
            // conversation. E with no exact hit soft-locks the NEAREST interactable within
            // reach that sits in front of the player (60-degree cone).
            if (EmberInput.Interact)
            {
                var soft = FindNearestInFront();
                if (soft != null) OpenDialog(soft, soft.transform.position);
            }
        }

        private EmberInteractable FindNearestInFront()
        {
            var candidates = Physics.OverlapSphere(_eye.position, _interactDistance, ~(1 << 2));
            EmberInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                var interactable = candidates[i].GetComponentInParent<EmberInteractable>();
                if (interactable == null) continue;
                var to = interactable.transform.position - _eye.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.01f || dist >= bestDist) continue;
                var flatForward = _eye.forward; flatForward.y = 0f;
                if (Vector3.Angle(flatForward, to) > 60f) continue; // behind or far off-axis: not intended
                best = interactable;
                bestDist = dist;
            }
            return best;
        }

        private void UpdateDofFocus()
        {
            if (_dof == null) return;
            _dof.active = true;
            // Simple focus on a fixed distance or raycast hit
            _dof.focusDistance.Override(EmberRuntimeOptionsProvider.Current.Interaction.DofFocusDistance);
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
                    // Route NPC dialogue to the redesigned in-game DialogView when the new UI controller is
                    // present (it owns the UI-Toolkit stage, frees the cursor + pauses, and polls the source for
                    // the async reply); fall through to the legacy DialogBoxPanel only if it is absent.
                    var inGameUi = FindFirstObjectByType<InGameUiController>(FindObjectsInactive.Include);
                    if (inGameUi != null)
                    {
                        if (target.HasActorId) commands.TryInteract(target.ActorId);
                        else commands.TryInteract(target.DisplayName);
                        var routedSource = target.HasActorId
                            ? commands.GetDialogSource(target.ActorId)
                            : commands.GetDialogSource(target.DisplayName);
                        if (routedSource != null)
                        {
                            if (_dof != null)
                            {
                                float dist = Vector3.Distance(_eye.position, hitPoint);
                                _dof.focusDistance.Override(dist);
                                _dof.active = true;
                            }
                            inGameUi.OpenNpcDialog(routedSource, target.DisplayName,
                                (routedSource as IDialogSourcePortrait)?.GetPortraitName(),
                                target.transform);
                            return;
                        }
                    }

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

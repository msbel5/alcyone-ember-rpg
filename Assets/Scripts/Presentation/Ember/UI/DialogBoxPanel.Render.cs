// REF-d (LEFT-020): per-frame show/hide (open animation + close) split out of DialogBoxPanel.cs (partial, zero behaviour change).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.Bootstrap;
using EmberCrpg.Presentation.Ember.Inputs;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed partial class DialogBoxPanel
    {
        private void OnEnable()
        {
            StartCoroutine(UiAnimationHelper.AnimateOpen(_canvasGroup, GetComponent<RectTransform>()));
            _typewriterElapsed = 0f;
            _displayedLineText = string.Empty;
            _isTypewriting = true;
        }

        public void Close()
        {
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            yield return UiAnimationHelper.AnimateClose(_canvasGroup, GetComponent<RectTransform>());
            Source = null;
            gameObject.SetActive(false);

            var pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            var pauseOpen = pause != null && pause.IsPaused;
            if (DialogCursorPolicy.ShouldLockAfterDialogClose(EmberWorldHost.IsModalOpen(), pauseOpen))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}

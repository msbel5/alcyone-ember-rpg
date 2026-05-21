using System.Collections;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI
{
    public static class UiAnimationHelper
    {
        public static IEnumerator AnimateOpen(CanvasGroup group, RectTransform rect, float duration = 0.18f)
        {
            float elapsed = 0f;
            group.alpha = 0f;
            rect.localScale = Vector3.one * 0.92f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float ease = EaseOutCubic(t);

                group.alpha = ease;
                rect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, ease);
                yield return null;
            }

            group.alpha = 1f;
            rect.localScale = Vector3.one;
        }

        public static IEnumerator AnimateClose(CanvasGroup group, RectTransform rect, float duration = 0.12f)
        {
            float elapsed = 0f;
            float startAlpha = group.alpha;
            Vector3 startScale = rect.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float ease = EaseInQuad(t);

                group.alpha = Mathf.Lerp(startAlpha, 0f, ease);
                rect.localScale = Vector3.one * Mathf.Lerp(startScale.x, 0.94f, ease);
                yield return null;
            }

            group.alpha = 0f;
            rect.localScale = Vector3.one * 0.94f;
        }

        private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
        private static float EaseInQuad(float x) => x * x;
    }
}

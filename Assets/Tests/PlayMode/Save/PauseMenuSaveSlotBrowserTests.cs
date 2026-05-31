using System.Collections;
using EmberCrpg.Presentation.Ember.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmberCrpg.Tests.PlayMode.Save
{
    public sealed class PauseMenuSaveSlotBrowserTests
    {
        [UnityTest]
        public IEnumerator PauseMenu_BuildsSlotBrowserButtons()
        {
            var canvas = new GameObject("PauseCanvas", typeof(Canvas));
            var menu = new GameObject("PauseMenu", typeof(RectTransform), typeof(CanvasGroup), typeof(PauseMenu));
            menu.transform.SetParent(canvas.transform, false);

            yield return null;

            Assert.That(FindChild(menu.transform, "PREV SLOT"), Is.Not.Null);
            Assert.That(FindChild(menu.transform, "NEXT SLOT"), Is.Not.Null);
            Assert.That(FindChild(menu.transform, "SAVE SLOT"), Is.Not.Null);
            Assert.That(FindChild(menu.transform, "LOAD SLOT"), Is.Not.Null);
            Assert.That(FindChild(menu.transform, "DELETE SLOT"), Is.Not.Null);

            Object.DestroyImmediate(menu);
            Object.DestroyImmediate(canvas);
        }

        private static Transform FindChild(Transform root, string name)
        {
            var children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i].name == name) return children[i];
            return null;
        }
    }
}

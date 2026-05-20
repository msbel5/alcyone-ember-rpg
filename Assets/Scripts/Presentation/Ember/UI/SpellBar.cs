using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    public interface ISpellBarSource
    {
        IReadOnlyList<string> GetSlots();
        int GetSelectedSlot();
    }

    /// <summary>
    /// Displays 5 spell slots. Highlights the currently selected slot.
    /// </summary>
    public sealed class SpellBar : MonoBehaviour
    {
        public ISpellBarSource Source { get; set; }
        public ISpriteByName SpriteLookup { get; set; }

        private readonly List<Image> _icons = new List<Image>();
        private readonly List<Outline> _outlines = new List<Outline>();

        private void Awake()
        {
            // Build 5 slots
            for (int i = 0; i < 5; i++)
            {
                var slot = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image), typeof(Outline));
                slot.transform.SetParent(transform, false);
                
                var rt = slot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(i * 0.2f, 0f);
                rt.anchorMax = new Vector2((i + 1) * 0.2f, 1f);
                rt.offsetMin = new Vector2(4f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);

                var img = slot.GetComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                _icons.Add(img);

                var outline = slot.GetComponent<Outline>();
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(3f, 3f);
                outline.enabled = false;
                _outlines.Add(outline);
                
                // Icon child
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(slot.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(4f, 4f);
                iconRt.offsetMax = new Vector2(-4f, -4f);
                
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
            }
        }

        private void Update()
        {
            if (Source == null || SpriteLookup == null) return;

            var slots = Source.GetSlots();
            int selected = Source.GetSelectedSlot();

            for (int i = 0; i < 5; i++)
            {
                var iconGo = _icons[i].transform.Find("Icon");
                if (iconGo == null) continue;
                
                var iconImg = iconGo.GetComponent<Image>();
                
                if (i < slots.Count)
                {
                    var sprite = SpriteLookup.GetSprite(slots[i]);
                    iconImg.sprite = sprite;
                    iconImg.enabled = sprite != null;
                }
                else
                {
                    iconImg.enabled = false;
                }

                _outlines[i].enabled = (i == selected);
            }
        }
    }
}

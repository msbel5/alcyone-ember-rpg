using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// F27: a tiny generated pictogram above a civilian billboard telling what they are DOING —
    /// a HAMMER over workers during work hours, a MUG over everyone during the midday meal
    /// (12:00-13:59, matching ScheduleSystem's lunch window). Same root-parented camera-facing
    /// family as the hostile marker; sprites are 12×12 pixel masks, generated once.
    /// </summary>
    public sealed class NpcPoseIconView : MonoBehaviour
    {
        private static Sprite s_hammer;
        private static Sprite s_mug;

        private SpriteRenderer _icon;
        private bool _worker;
        private float _nextPoll;

        public void Bind(bool workerRole)
        {
            _worker = workerRole;
            var go = new GameObject("PoseIcon");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            go.transform.localScale = Vector3.one * 0.5f;
            _icon = go.AddComponent<SpriteRenderer>();
            go.AddComponent<CameraFacingBillboard>();
            _icon.enabled = false;
        }

        private void Update()
        {
            if (_icon == null || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 1.1f; // the hour changes slowly — poll lazily

            int hour = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.HourOfDay;
            bool lunch = hour >= 12 && hour < 14;
            bool work = _worker && hour >= 8 && hour < 18 && !lunch;

            if (lunch) { _icon.sprite = Mug(); _icon.enabled = true; }
            else if (work) { _icon.sprite = Hammer(); _icon.enabled = true; }
            else _icon.enabled = false;
        }

        // ----- generated pictograms ------------------------------------------------------------

        private static Sprite Hammer()
        {
            if (s_hammer == null)
                s_hammer = FromMask(new[]
                {
                    "............",
                    "..########..",
                    "..########..",
                    "..########..",
                    "......##....",
                    "......##....",
                    "......##....",
                    "......##....",
                    "......##....",
                    "......##....",
                    "......##....",
                    "............",
                }, new Color(0.62f, 0.44f, 0.22f), new Color(0.75f, 0.75f, 0.80f));
            return s_hammer;
        }

        private static Sprite Mug()
        {
            if (s_mug == null)
                s_mug = FromMask(new[]
                {
                    "............",
                    "..#######...",
                    "..#######.#.",
                    "..#######.#.",
                    "..########..",
                    "..#######...",
                    "..#######...",
                    "..#######...",
                    "...#####....",
                    "............",
                    "............",
                    "............",
                }, new Color(0.92f, 0.70f, 0.25f), new Color(0.92f, 0.70f, 0.25f));
            return s_mug;
        }

        // '#' rows 0-3 take the HEAD colour, the rest the BODY colour — enough for both glyphs.
        private static Sprite FromMask(string[] rows, Color body, Color head)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool on = rows[y][x] == '#';
                    var c = y < 4 ? head : body;
                    tex.SetPixel(x, h - 1 - y, on ? c : Color.clear);
                }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), pixelsPerUnit: w);
        }
    }
}

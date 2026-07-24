using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// F27: a tiny generated pictogram above a civilian billboard telling what they are DOING —
    /// a HAMMER over workers during work hours, a MUG while the actor's CurrentAction is
    /// ConsumeFood (W32 DOC5: the mug reads the ACTION, not the clock). Same root-parented
    /// camera-facing family as the hostile marker; sprites are 12×12 pixel masks, generated once.
    /// </summary>
    public sealed class NpcPoseIconView : MonoBehaviour
    {
        private static Sprite s_hammer;
        private static Sprite s_mug;

        private SpriteRenderer _icon;
        private bool _worker;
        private float _nextPoll;
        private string _actionKind;

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

        /// <summary>W32 DOC5: pushed by ActorView.SetTarget — the same feed as the activity label.
        /// The view may branch on the kind; it may NOT re-derive it from hour/position.</summary>
        public void SetActionKind(string kind) => _actionKind = kind;

        private void Update()
        {
            if (_icon == null || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 1.1f; // the hour changes slowly — poll lazily

            bool eating = _actionKind == "ConsumeFood"; // verbatim action read — no lunch-window guess
            // GUESS(WORK slice): retire when PerformWorkAction lands — hour poll survives for this branch only.
            int hour = EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.HourOfDay;
            bool work = _worker && hour >= 8 && hour < 18 && !eating;

            if (eating) { _icon.sprite = Mug(); _icon.enabled = true; }
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

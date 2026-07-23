using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Views
{
    /// <summary>
    /// M6: shows the actor's latest REAL event as a 12x12 pictogram for a few seconds -
    /// eye = saw something, ! = reported it, sword = guard answered, sheaf = harvested,
    /// dots = talked. Same root-parented camera-facing family as the pose icons; masks are
    /// symmetric so CameraFacingBillboard mirroring cannot misread them.
    /// </summary>
    public sealed class NpcEventEchoView : MonoBehaviour
    {
        private static Sprite s_eye, s_alert, s_sword, s_sheaf, s_chat;

        private ulong _actorId;
        private SpriteRenderer _icon;
        private int _seenStamp;
        private float _hideAt;
        private float _nextPoll;

        public void Bind(ulong actorId)
        {
            _actorId = actorId;
            var go = new GameObject("EventEcho");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(0.55f, 3.15f, 0f);
            go.transform.localScale = Vector3.one * 0.5f;
            _icon = go.AddComponent<SpriteRenderer>();
            go.AddComponent<CameraFacingBillboard>();
            _icon.enabled = false;
            _seenStamp = EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.Stamp;
        }

        private void Update()
        {
            if (_icon == null) return;
            if (_icon.enabled && Time.unscaledTime >= _hideAt) _icon.enabled = false;
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.4f;

            int kind = EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.LatestKindFor(_actorId, _seenStamp);
            _seenStamp = EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.Stamp;
            if (kind < 0) return;
            _icon.sprite = SpriteFor(kind);
            _icon.enabled = true;
            _hideAt = Time.unscaledTime + 3.5f;
        }

        private static Sprite SpriteFor(int kind)
        {
            switch (kind)
            {
                case EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindReport:
                    return s_alert != null ? s_alert : (s_alert = FromMask(AlertMask, new Color(0.95f, 0.55f, 0.15f)));
                case EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindGuard:
                    return s_sword != null ? s_sword : (s_sword = FromMask(SwordMask, new Color(0.85f, 0.20f, 0.15f)));
                case EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindHarvest:
                    return s_sheaf != null ? s_sheaf : (s_sheaf = FromMask(SheafMask, new Color(0.83f, 0.68f, 0.22f)));
                case EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindTalk:
                    return s_chat != null ? s_chat : (s_chat = FromMask(ChatMask, new Color(0.85f, 0.82f, 0.72f)));
                default:
                    return s_eye != null ? s_eye : (s_eye = FromMask(EyeMask, new Color(0.75f, 0.85f, 0.95f)));
            }
        }

        private static readonly string[] EyeMask =
        {
            "............", "............", "...######...", ".##......##.",
            "##..####..##", "##.######.##", "##..####..##", ".##......##.",
            "...######...", "............", "............", "............",
        };
        private static readonly string[] AlertMask =
        {
            "....####....", "....####....", "....####....", "....####....",
            "....####....", ".....##.....", ".....##.....", "............",
            ".....##.....", "....####....", ".....##.....", "............",
        };
        private static readonly string[] SwordMask =
        {
            ".....##.....", ".....##.....", ".....##.....", ".....##.....",
            ".....##.....", ".....##.....", "..########..", ".....##.....",
            ".....##.....", "....####....", "............", "............",
        };
        private static readonly string[] SheafMask =
        {
            "..#..##..#..", "..#..##..#..", "...#.##.#...", "...#.##.#...",
            "....####....", ".....##.....", ".....##.....", ".....##.....",
            "....####....", "...######...", "............", "............",
        };
        private static readonly string[] ChatMask =
        {
            "............", "............", "..........", "............",
            "..##.##.##..", "..##.##.##..", "............", "............",
            "............", "............", "............", "............",
        };

        private static Sprite FromMask(string[] rows, Color color)
        {
            int h = rows.Length, w = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool on = x < rows[y].Length && rows[y][x] == '#';
                    tex.SetPixel(x, h - 1 - y, on ? color : Color.clear);
                }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 12f);
        }
    }
}

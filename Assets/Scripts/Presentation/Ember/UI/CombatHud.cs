using UnityEngine;
using UnityEngine.UI;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Bottom-bar combat HUD. Renders three horizontal bars (health, stamina, mana) and
    /// a damage log line. Reads from an injected <see cref="ICombatHudSource"/>;
    /// nothing simulation-specific lives in the view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour
    {
        public ICombatHudSource Source { get; set; }

        private Bar _health;
        private Bar _stamina;
        private Bar _mana;
        private Text _damageLog;

        private void Awake()
        {
            _health      = Bar.Build(transform, "Health",  new Vector2(0.02f, 0.55f), new Vector2(0.32f, 0.85f), new Color(0.85f, 0.2f, 0.15f));
            // Codex audit (sixth pass J-P3 #J2): the bar previously labeled
            // "Stamina" actually displays the domain's Vitals.Fatigue value.
            // Renamed the visible label to match the domain stat so players
            // see the same concept the simulation tracks. Internal field name
            // (`_stamina`) and CombatHudState parameter (`stamina`) stay as-is
            // for now — staged rename, full alignment scheduled with the Faz
            // 13 cleanup sprint.
            _stamina     = Bar.Build(transform, "Fatigue", new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.85f), new Color(0.85f, 0.7f, 0.1f));
            _mana        = Bar.Build(transform, "Mana",    new Vector2(0.68f, 0.55f), new Vector2(0.98f, 0.85f), new Color(0.2f, 0.45f, 0.95f));
            _damageLog   = BuildLogLine(transform, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.5f));
        }

        private void Update()
        {
            if (Source == null) return;
            var s = Source.Read();
            _health.SetRatio (Ratio(s.Health,  s.HealthMax));
            _stamina.SetRatio(Ratio(s.Stamina, s.StaminaMax));
            _mana.SetRatio   (Ratio(s.Mana,    s.ManaMax));
            _damageLog.text = string.IsNullOrEmpty(s.LastEventLine) ? "—" : s.LastEventLine;
        }

        private static float Ratio(int value, int max) => max > 0 ? Mathf.Clamp01((float)value / max) : 0f;

        private static Text BuildLogLine(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("DamageLog", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(8f, 4f);
            rt.offsetMax = new Vector2(-8f, -4f);
            var text = go.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = new Color(0.95f, 0.95f, 0.88f);
            return text;
        }

        private sealed class Bar
        {
            private RectTransform _fill;

            public static Bar Build(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color)
            {
                var bar = new Bar();
                var go = new GameObject(label, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, worldPositionStays: false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(go.transform, worldPositionStays: false);
                bar._fill = fillGo.GetComponent<RectTransform>();
                bar._fill.anchorMin = Vector2.zero;
                bar._fill.anchorMax = new Vector2(1f, 1f);
                bar._fill.offsetMin = new Vector2(2f, 2f);
                bar._fill.offsetMax = new Vector2(-2f, -2f);
                fillGo.GetComponent<Image>().color = color;
                bar.SetRatio(1f);
                return bar;
            }

            public void SetRatio(float ratio)
            {
                var clamped = Mathf.Clamp01(ratio);
                _fill.anchorMax = new Vector2(clamped, 1f);
            }
        }
    }

    public readonly struct CombatHudState
    {
        public readonly int Health, HealthMax;
        public readonly int Stamina, StaminaMax;
        public readonly int Mana, ManaMax;
        public readonly string LastEventLine;
        public CombatHudState(int health, int healthMax, int stamina, int staminaMax, int mana, int manaMax, string lastEventLine)
        {
            Health = health; HealthMax = healthMax;
            Stamina = stamina; StaminaMax = staminaMax;
            Mana = mana; ManaMax = manaMax;
            LastEventLine = lastEventLine;
        }
    }

    public interface ICombatHudSource
    {
        CombatHudState Read();
    }
}

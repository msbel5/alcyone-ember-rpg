using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>F26: the realize step records where the functional interiors stand — proof anchors
    /// and the innkeeper pin read these (one writer: WorldSceneDirector).</summary>
    public static class RuntimeInteriorInfo
    {
        public static Vector3 TavernWorld { get; private set; }
        public static Vector3 TempleWorld { get; private set; }
        public static Vector3 ShopWorld { get; private set; }

        public static void Record(Vector3 tavern, Vector3 temple, Vector3 shop)
        {
            TavernWorld = tavern;
            TempleWorld = temple;
            ShopWorld = shop;
        }
    }

    /// <summary>F26: one-shot screen request from world props to the UI controller (the same
    /// one-flag-one-consumer shape as WorldEncounterSignal).</summary>
    public static class ScreenRequestSignal
    {
        private static string s_pending;
        public static void Request(string screen) => s_pending = screen;
        public static string Consume() { var p = s_pending; s_pending = null; return p; }
    }

    /// <summary>F26 TAVERN: E inside (≤2.6m of the hearth anchor) → sleep 8 hours for 5 gold —
    /// vitals refill, the world clock walks forward hour-by-hour.</summary>
    public sealed class RuntimeTavernView : MonoBehaviour
    {
        private Transform _player;
        private float _nextPoll;

        private void Update()
        {
            if (!FunctionalInteriorTrigger.Poll(transform, ref _player, ref _nextPoll)) return;
            if (EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter)
            {
                adapter.LogCombat(adapter.TrySleepAtTavern());
                RuntimeAudioDirector.PlayUiClick();
            }
        }
    }

    /// <summary>F26 TEMPLE: E inside → the clergy mend your wounds for 8 gold (health only).</summary>
    public sealed class RuntimeTempleView : MonoBehaviour
    {
        private Transform _player;
        private float _nextPoll;

        private void Update()
        {
            if (!FunctionalInteriorTrigger.Poll(transform, ref _player, ref _nextPoll)) return;
            if (EmberDomainAdapterLocator.Current is DomainSimulationAdapter adapter)
            {
                adapter.LogCombat(adapter.TryTempleHeal());
                RuntimeAudioDirector.PlayUiClick();
            }
        }
    }

    /// <summary>F26 SHOP: E at the counter → the trade screen opens (signal to the controller).</summary>
    public sealed class RuntimeShopCounterView : MonoBehaviour
    {
        private Transform _player;
        private float _nextPoll;

        private void Update()
        {
            if (!FunctionalInteriorTrigger.Poll(transform, ref _player, ref _nextPoll)) return;
            ScreenRequestSignal.Request("trade");
            Debug.Log("[Shop] counter used — trade screen requested.");
        }
    }

    /// <summary>Shared proximity + E trigger for the functional interiors (chest-view family).</summary>
    internal static class FunctionalInteriorTrigger
    {
        public static bool Poll(Transform self, ref Transform player, ref float nextPoll)
        {
            if (Time.unscaledTime >= nextPoll)
            {
                nextPoll = Time.unscaledTime + 0.4f;
                if (player == null)
                {
                    var rig = GameObject.Find("PlayerRig");
                    player = rig != null ? rig.transform : null;
                }
            }
            if (player == null) return false;
            var delta = player.position - self.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 2.6f * 2.6f) return false;
            return EmberCrpg.Presentation.Ember.Inputs.EmberInput.Interact;
        }
    }
}

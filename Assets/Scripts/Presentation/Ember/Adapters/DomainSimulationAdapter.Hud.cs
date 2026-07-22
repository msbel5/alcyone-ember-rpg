using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Presentation.Ember.UI;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // ----- IEmberHudReadModel -----
        public string HudText
        {
            get
            {
                var day = 1 + _tick / EmberCrpg.Simulation.Composition.WorldTickComposer.TicksPerGameDay;
                var profile = _world.WorldProfile;
                // Anchor the player in the real generated world: name the settlement they started in.
                var town = ResolveStartingSettlementName();
                // PERF ("oyun her tickte kasıyor" fix, part 1): the HUD binds every frame but this string only
                // changes per tick — rebuilding it (plus the settlement scan inside ResolvePlayerOverlandTile)
                // every frame was pure waste.
                if (_hudTextTick == _tick && _hudTextCache != null)
                    return _hudTextCache;
                _hudTextTick = _tick;

                var where = string.IsNullOrEmpty(town) ? string.Empty : $"   •   {town}";
                // Player overland tile on the HUD: the map, the side panel and the 3D world all key off this
                // coordinate, so showing it is the player's ground-truth anchor ("where exactly am I?").
                if (GeneratedWorld != null && _world?.Overland != null)
                {
                    var tile = ResolvePlayerOverlandTile();
                    where += $"   •   Tile {tile.X},{tile.Y}";
                }
                // F23: standing and trouble, both on the bar — rep (when it moved) and the watch's bounty.
                if (_world != null && _world.PlayerReputation != 0)
                    where += $"   •   Rep {_world.PlayerReputation:+0;-0}";
                if (_world != null && _world.PlayerBountyGold > 0)
                    where += $"   •   BOUNTY {_world.PlayerBountyGold}g";
                var clockText = _world != null
                    ? $"   {_world.Time.Hour:00}:{_world.Time.Minute:00}"
                    : string.Empty;
                if (profile == null)
                    return _hudTextCache = $"Tick {_tick:0000}   Day {day:000}{clockText}{where}";
                // Population is the world the HISTORY simulated (sum of surviving settlement populations),
                // not the static TargetPopulation knob, so the number reflects centuries of growth/decline.
                var population = GeneratedWorld != null ? GeneratedWorld.TotalPopulation : profile.TargetPopulation;
                return _hudTextCache = $"Tick {_tick:0000}   Day {day:000}{clockText}   {Spaced(profile.Style)} / {Spaced(profile.Genre)}   Pop {population:N0}{where}";
            }
        }

        private string _hudTextCache;
        private int _hudTextTick = -1;

        // Render a CamelCase enum value as spaced Title Case for the HUD ("LowFantasy" -> "Low Fantasy").
        // The brand codenames were renamed out of the WorldStyle enum (BUG-1), so this is pure display polish.
        private static string Spaced(System.Enum value)
        {
            var s = value.ToString();
            return System.Text.RegularExpressions.Regex.Replace(s, "(?<=[a-z0-9])(?=[A-Z])", " ");
        }

        public CombatHudState CombatHud
        {
            get
            {
                var player = _world.Actors.FirstByRole(ActorRole.Player);
                if (player == null) return new CombatHudState(0, 100, 0, 100, 0, 100, _lastCombatLine);
                var v = player.Vitals;
                return new CombatHudState(
                    v.Health.Current, v.Health.Max,
                    v.Fatigue.Current, v.Fatigue.Max,
                    v.Mana.Current, v.Mana.Max,
                    _lastCombatLine);
            }
        }

        public PlayerSheetState PlayerSheet
        {
            get
            {
                var player = _world.Actors?.FirstByRole(ActorRole.Player);
                if (player == null) return default;   // HasData = false → CharacterView keeps its mock defaults
                var s = player.Stats;
                return new PlayerSheetState(player.Name, s.Mig, s.Agi, s.End, s.Mnd, s.Ins, s.Pre);
            }
        }

    }
}

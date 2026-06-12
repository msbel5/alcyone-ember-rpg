using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Magic;

namespace EmberCrpg.Simulation.World
{
    public readonly struct PlayerLevelUpChoice
    {
        public PlayerLevelUpChoice(int migDelta, int agiDelta, int endDelta, int mndDelta, int insDelta, int preDelta, string selectedSpellId)
        {
            MigDelta = migDelta;
            AgiDelta = agiDelta;
            EndDelta = endDelta;
            MndDelta = mndDelta;
            InsDelta = insDelta;
            PreDelta = preDelta;
            SelectedSpellId = selectedSpellId ?? string.Empty;
        }

        public int MigDelta { get; }
        public int AgiDelta { get; }
        public int EndDelta { get; }
        public int MndDelta { get; }
        public int InsDelta { get; }
        public int PreDelta { get; }
        public string SelectedSpellId { get; }
        public int TotalPoints => MigDelta + AgiDelta + EndDelta + MndDelta + InsDelta + PreDelta;
    }

    public sealed class PlayerLevelUpService
    {
        public const int PointsPerLevel = 5;

        /// <summary>F17: the XP price of the NEXT level — level N -> N+1 costs N*100 (kill=40, quest=60).</summary>
        public static int XpForNextLevel(int currentLevel) => System.Math.Max(1, currentLevel) * 100;

        public bool TryApply(WorldState world, PlayerLevelUpChoice choice, out string message)
        {
            if (world == null)
            {
                message = "Level-up state is unavailable.";
                return false;
            }

            var player = world.Actors?.FirstByRole(ActorRole.Player);
            if (player == null)
            {
                message = "No player actor is available.";
                return false;
            }

            if (choice.TotalPoints != PointsPerLevel)
            {
                message = "Spend exactly " + PointsPerLevel + " points before confirming.";
                return false;
            }

            // F17 XP GATE: leveling is EARNED — the screen used to allow infinite level-ups.
            int xpCost = XpForNextLevel(world.PlayerLevel);
            if (world.PlayerXp < xpCost)
            {
                message = "Not enough experience: " + world.PlayerXp + "/" + xpCost + " XP.";
                return false;
            }

            var learnedSpellName = "No new spell";
            if (!string.IsNullOrWhiteSpace(choice.SelectedSpellId) && WorldSpellCatalog.Find(choice.SelectedSpellId) == null)
            {
                message = "Unknown spell selection.";
                return false;
            }

            try
            {
                var updatedStats = new EmberStatBlock(
                    player.Stats.Mig + choice.MigDelta,
                    player.Stats.Agi + choice.AgiDelta,
                    player.Stats.End + choice.EndDelta,
                    player.Stats.Mnd + choice.MndDelta,
                    player.Stats.Ins + choice.InsDelta,
                    player.Stats.Pre + choice.PreDelta);
                // F28 MANA ECONOMY: Mind grows the mana pool (+2 max per Mnd point, the gain
                // arrives FILLED). Without this the 12-point loadout pool could never cast
                // ember_ward (15), frost_lance (17) or recall_gate (20) — the school stayed
                // sealed at the starter spells no matter how many levels were earned.
                var updatedVitals = player.Vitals;
                if (choice.MndDelta > 0)
                {
                    var manaGain = choice.MndDelta * 2;
                    updatedVitals = updatedVitals.WithMana(new VitalStat(
                        updatedVitals.Mana.Current + manaGain,
                        updatedVitals.Mana.Max + manaGain));
                }
                world.ReplaceActorView(ActorRole.Player, CloneWithStats(player, updatedStats, updatedVitals));
            }
            catch (ArgumentOutOfRangeException)
            {
                message = "Level-up would push a stat out of bounds.";
                return false;
            }

            world.PlayerXp -= xpCost; // spend the earned XP (leftover rolls toward the next level)
            world.PlayerLevel = Math.Max(1, world.PlayerLevel + 1);
            world.PlayerKnownSpellIds ??= new List<string>();

            if (!string.IsNullOrWhiteSpace(choice.SelectedSpellId))
            {
                var spell = WorldSpellCatalog.Find(choice.SelectedSpellId);
                if (!world.PlayerKnownSpellIds.Contains(choice.SelectedSpellId))
                    world.PlayerKnownSpellIds.Add(choice.SelectedSpellId);
                learnedSpellName = spell.DisplayName ?? choice.SelectedSpellId;
            }

            message = "Level " + world.PlayerLevel + " attained. " + learnedSpellName + ".";
            world.LastNarrative = message;
            world.Events?.Append(new WorldEvent(
                world.Time,
                WorldEventKind.StorytellerCheckpoint,
                player.Id,
                default,
                "level_up spell:" + (choice.SelectedSpellId ?? string.Empty) + " points:" + choice.TotalPoints));
            return true;
        }

        private static ActorRecord CloneWithStats(ActorRecord source, EmberStatBlock stats, ActorVitals vitals)
        {
            var copy = new ActorRecord(
                source.Id,
                source.Name,
                source.Role,
                stats,
                vitals,
                source.Position,
                source.Accuracy,
                source.Dodge,
                source.Armor,
                source.BaseDamage,
                source.TopicIds,
                source.JobPreferences,
                source.ScheduleState,
                source.Needs,
                source.Mood,
                source.Memory,
                source.Home,
                source.DayAnchor);
            copy.ReplaceAskedTopics(source.AskedTopicIds);
            return copy;
        }
    }
}

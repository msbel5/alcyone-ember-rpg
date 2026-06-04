// CharCreation shared data + helpers + the story-launch coroutine. The old string-slot _panel rendering that
// used to live here (Build*Body string builders, Render*Buttons, StepLabel, FormatStat, …) was deleted once the
// UI-Toolkit redesign — CharCreationToolkitView, routed in .Redesign.cs — covered all 11 steps. Every step now
// renders through that view; what remains here is the catalog data + helpers the redesign reuses.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        private void Render()
        {
            // Every step renders through the redesigned UI-Toolkit view (routed in .Redesign.cs). If there is no
            // redesign view (no UI-Toolkit surface) there is simply nothing to draw — the legacy string-slot
            // _panel renderer was removed once the redesign covered all 11 steps.
            if (_redesignView != null)
                RenderRedesign();
        }

        private IEnumerator BeginVisibleWorldgen()
        {
            LoadingScreen.ShowForContext(new LoadingScreenContext("worldgen", "Entering the World", "generation"));
            var style = MoodToStyle(_worldMood);
            var genre = CallingToGenre(_playerCalling);
            LoadingScreen.LogLine(UiLogSeverity.Info, "[worldgen] seed=" + _seed + " class=" + _selectedClassId
                + " mood=" + _worldMood + " calling=" + _playerCalling + " fate=" + _fateStart
                + " => style=" + style + " genre=" + genre);
            LoadingScreen.SetProgress(0.6f, "Weaving your world from the embers");
            yield return null;

            // DO NOT mount the visible worldgen reveal panel here. It was deliberately removed because it
            // flashed past before the player could read it AND its "where should the commander begin?"
            // start-scene question is NOT wired to the actual start location (it loaded the same scene
            // regardless). f3f6ef28 re-added it via AI drift — this re-removes it. The authoritative world
            // is generated in the TARGET SCENE from EmberWorldGenIntent.Pending. Keep this straight-to-game.
            LoadingScreen.SetProgress(1f, "Entering the generated world");
            yield return new WaitForSecondsRealtime(0.5f);
            // World pivot: ALWAYS enter the runtime-generated world. The _firstSceneName plumbing (and the
            // CharacterCreationUI [SerializeField] baked as "SmithingOverworld", which calls SetStartScene and
            // overrode the controller default) is bypassed here so New Game never lands in a baked
            // vertical-slice scene again — the World Scene Director builds the starting settlement instead.
            UnityEngine.SceneManagement.SceneManager.LoadScene(EmberScenes.GeneratedWorld);
        }

        // ----- World-genesis choices (stages 2-4). The chosen id feeds worldgen: mood -> WorldStyle,
        // calling -> WorldGenre (MoodToStyle / CallingToGenre below), fate -> start-locale flavor (logged in
        // BeginVisibleWorldgen). Consumed by the redesign's choice-list screens in .Redesign.cs. Controller
        // defaults keep Continue enabled before a pick, so the flow never locks on these stages.
        private static readonly (string id, string label)[] MoodChoices =
        {
            ("grim", "Grim and unforgiving - a dying age of ash"),
            ("mythic", "Mythic and ancient - the old gods still stir"),
            ("low", "Gritty and low - mud, steel, and rumor"),
            ("heroic", "High and heroic - banners raised against the dark"),
        };

        private static readonly (string id, string label)[] CallingChoices =
        {
            ("survival", "Endure the wilds - survival above all"),
            ("intrigue", "Play the courts - politics and quiet knives"),
            ("hunt", "Hunt what stalks - monsters in the dark"),
            ("merchant", "Build a fortune - trade roads and coin"),
            ("pilgrimage", "Walk the long road - faith, ruin, and relics"),
        };

        private static readonly (string id, string label)[] FateChoices =
        {
            ("forge", "At the forge - hammer, heat, and a trade to your name"),
            ("tavern", "In the tavern - a stranger, a job, and a debt"),
            ("crossroads", "At a crossroads - the open road and no master"),
        };

        // World-genesis id -> worldgen enums. Unknown / default ids fall to the grim-survival baseline so a
        // never-touched stage still yields a coherent world.
        private static WorldStyle MoodToStyle(string mood)
        {
            return WorldGenesisMapper.ToStyle(mood);
        }

        private static WorldGenre CallingToGenre(string calling)
        {
            return WorldGenesisMapper.ToGenre(string.Empty, calling, string.Empty);
        }

        private static int SafeStat(IReadOnlyDictionary<string, int> stats, string key)
        {
            return stats.TryGetValue(key, out var value) ? value : 0;
        }

        private static string AlignmentName(string id)
        {
            switch ((id ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "lawful_good": return "Lawful Good";
                case "neutral_good": return "Neutral Good";
                case "chaotic_good": return "Chaotic Good";
                case "lawful_neutral": return "Lawful Neutral";
                case "true_neutral": return "True Neutral";
                case "chaotic_neutral": return "Chaotic Neutral";
                case "lawful_evil": return "Lawful Evil";
                case "neutral_evil": return "Neutral Evil";
                case "chaotic_evil": return "Chaotic Evil";
                default: return "Unaligned";
            }
        }

        private static string ClassName(string classId)
        {
            foreach (var klass in CharacterCreationCatalog.Classes)
            {
                if (string.Equals(klass.Id, classId, StringComparison.OrdinalIgnoreCase))
                    return klass.Name;
            }
            return classId;
        }

        private static string DominantStats(CharacterClass klass)
        {
            var scores = new[]
            {
                new KeyValuePair<string, int>("MIG", klass.PrimaryStats.Mig),
                new KeyValuePair<string, int>("AGI", klass.PrimaryStats.Agi),
                new KeyValuePair<string, int>("END", klass.PrimaryStats.End),
                new KeyValuePair<string, int>("MND", klass.PrimaryStats.Mnd),
                new KeyValuePair<string, int>("INS", klass.PrimaryStats.Ins),
                new KeyValuePair<string, int>("PRE", klass.PrimaryStats.Pre),
            };

            return string.Join(", ", scores.OrderByDescending(v => v.Value).Take(2).Select(v => v.Key));
        }

        private List<string> BuildSkillCatalog()
        {
            var list = new List<string>();
            foreach (var klass in CharacterCreationCatalog.Classes)
            {
                foreach (var skill in klass.MinorSkills)
                {
                    if (list.Contains(skill, StringComparer.OrdinalIgnoreCase)) continue;
                    list.Add(skill);
                }
            }

            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private static string LinkedAbility(string skill)
        {
            switch ((skill ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "athletics":
                case "intimidation":
                    return "MIG";
                case "stealth":
                case "sleight_of_hand":
                    return "AGI";
                case "survival":
                case "medicine":
                    return "END";
                case "arcana":
                case "history":
                case "religion":
                    return "MND";
                case "investigation":
                case "insight":
                case "perception":
                    return "INS";
                case "persuasion":
                case "deception":
                    return "PRE";
                default:
                    return "INS";
            }
        }
    }
}

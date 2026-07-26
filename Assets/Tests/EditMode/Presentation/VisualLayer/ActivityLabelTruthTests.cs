using System.IO;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Presentation.Ember.Adapters;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Presentation.VisualLayer
{
    /// <summary>
    /// W32 DOC6 T6 (RUH_TESHIS §10): the UI label is IDENTICAL to CurrentAction. The verb is
    /// the action's own declaration read verbatim through ActionVerbTable — the formatter has
    /// no clock/position/needs input BY SIGNATURE, so it cannot invent verbs. The lint half
    /// (GateContractLintTests source-reading pattern) pins the death of the EAT guess branches.
    /// </summary>
    public sealed class ActivityLabelTruthTests
    {
        [Test]
        public void Verb_IsTheActionsOwnDeclaration_PerPhase()
        {
            Assert.That(ActionVerbTable.Verb(ActorActionType.MoveToFood), Is.EqualTo("seeking food"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.TakeFood), Is.EqualTo("taking food"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.ConsumeFood), Is.EqualTo("eating"));
            // W33 F7: the farm verbs are TABLE rows born from real actions now — the projection's
            // crop-belt proximity guesses ("harvesting"/"tending the field") are dead branches.
            Assert.That(ActionVerbTable.Verb(ActorActionType.MoveToPlot), Is.EqualTo("to the field"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.PlantSeed), Is.EqualTo("planting"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.HarvestCrop), Is.EqualTo("harvesting"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.HaulCrop), Is.EqualTo("hauling"));
            // W34 S7: the SLEEP and WORK verbs are TABLE rows born from real actions — the
            // projection's clock+home guess ("sleeping"/"heading home"/"winding down") and its
            // schedule-derived "working" dies with the arrival of MoveToBed/Sleep/MoveToWorksite/
            // PerformWork actions the sim now TELLS the view.
            Assert.That(ActionVerbTable.Verb(ActorActionType.MoveToBed), Is.EqualTo("heading home"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.Sleep), Is.EqualTo("sleeping"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.MoveToWorksite), Is.EqualTo("to work"));
            Assert.That(ActionVerbTable.Verb(ActorActionType.PerformWork), Is.EqualTo("working"));

            // The signature IS the guarantee: one ActorActionType in, nothing else — the
            // formatter has no world/clock input to guess from (plaza+12:30 cannot say "eating").
            var verb = typeof(ActionVerbTable).GetMethod("Verb");
            Assert.That(verb.GetParameters().Select(p => p.ParameterType),
                Is.EqualTo(new[] { typeof(ActorActionType) }),
                "Verb may only derive from the action kind — a second input is the §2.9 disease");
        }

        [Test]
        public void ActionlessActor_HasNoActionLabel()
        {
            Assert.That(ActionVerbTable.KindName(ActorActionType.None), Is.Null,
                "no action -> no ActionKind — the schedule-word fallback owns actionless actors");
        }

        [Test]
        public void Lint_ProjectionReadsTheTable_AndTheEatGuessBranchesAreDead()
        {
            var root = RepoRoot();
            if (root == null)
            {
                Assert.Inconclusive("repo root not reachable from test runner cwd — lint skipped");
                return;
            }
            var path = Path.Combine(root, "Assets", "Scripts", "Presentation", "Ember",
                "Adapters", "DomainSimulationAdapter.WorldProjection.cs");
            var code = string.Join("\n", File.ReadAllLines(path)
                .Where(line => !line.TrimStart().StartsWith("//"))); // comments may TALK history

            Assert.That(code, Does.Contain("ActionVerbTable.Verb"),
                "the projection must read the ONE truth source");
            Assert.That(code, Does.Contain("ActionState.CurrentAction"),
                "the verb must be born from the actor's carried action");
            // W33 F7: the farm guesses join the banned list — those verbs may only be born from
            // MoveToPlot/PlantSeed/HarvestCrop/HaulCrop actions, and the retired GUESS(FARM)
            // tag may not linger (a landed slice leaves no surviving guess to tag).
            // W34 S7: the SLEEP+WORK guesses join too — the hour/needs sourced labels
            // ("sleeping"/"heading home"/"winding down"/"working") may only be born from
            // MoveToBed/Sleep/MoveToWorksite/PerformWork actions; the retired GUESS(SLEEP)
            // and GUESS(WORK) tags may not linger either.
            foreach (var banned in new[] { "to the tavern", "hour >= 12", "\"eating\"", "\"seeking food\"",
                "tending the field", "harvesting\"", "GUESS(FARM",
                "\"heading home\"", "\"winding down\"", "\"working\"", "IsAsleepAtHome",
                "GUESS(SLEEP", "GUESS(WORK" })
                Assert.That(code.Contains(banned), Is.False,
                    $"'{banned}' guess branch still lives in the projection — the view invents verbs");

            // W36 GUARD+COMBAT: the last two GUESS branches (Guard "on watch", Enemy "hunting")
            // are DEAD. DescribeScheduleWord has no surviving guess to tag — the lint's earlier
            // "no surviving guess" contract now supersedes the "surviving guesses must be
            // tagged" contract. Any regression that re-introduces a guess must (a) tag it
            // GUESS(<slice>) AND (b) update this assertion to Does.Contain again.
            Assert.That(code, Does.Not.Contain("GUESS("),
                "no surviving guess branches — verb births exclusively from ActionVerbTable now");
        }

        [Test]
        public void Lint_VerbTable_IsPureStaticData()
        {
            var root = RepoRoot();
            if (root == null)
            {
                Assert.Inconclusive("repo root not reachable from test runner cwd — lint skipped");
                return;
            }
            var code = string.Join("\n", File.ReadAllLines(Path.Combine(root, "Assets", "Scripts",
                    "Presentation", "Ember", "Adapters", "ActionVerbTable.cs"))
                .Where(line => !line.TrimStart().StartsWith("//")));
            foreach (var banned in new[] { "Hour", "Position", "Needs", "GameTime" })
                Assert.That(code.Contains(banned), Is.False,
                    $"ActionVerbTable reads '{banned}' — an hour/position/needs input here recreates §2.9");
        }

        private static string RepoRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            for (var i = 0; i < 8 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "Assets", "Tests", "EditMode", "Presentation")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}

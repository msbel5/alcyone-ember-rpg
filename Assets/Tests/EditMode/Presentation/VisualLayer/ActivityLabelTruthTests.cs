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
            foreach (var banned in new[] { "to the tavern", "hour >= 12", "\"eating\"", "\"seeking food\"" })
                Assert.That(code.Contains(banned), Is.False,
                    $"'{banned}' guess branch still lives in the projection — the view invents verbs");

            // Surviving non-EAT guesses must be tagged for their retiring slice (grep contract).
            Assert.That(code, Does.Contain("GUESS("),
                "surviving guess branches must carry the GUESS(<slice>) retirement tag");
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

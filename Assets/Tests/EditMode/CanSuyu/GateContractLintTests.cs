using System.IO;
using System.Linq;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CanSuyu
{
    /// <summary>
    /// CAN SUYU H5 — the lint half of the gate contract. The V1 proof system rotted because
    /// DoDs quietly regressed into choreography checks ("at 12:00 there are 8 actors at the
    /// tavern"). This lint makes that regression a CI failure: gate tests must sample the
    /// world OVER TIME (loops, waves, trajectories), and the roadmap's DoD lines may not
    /// promise fixed-hour or screenshot proofs. Screenshots stay supportive, never sufficient.
    /// </summary>
    public sealed class GateContractLintTests
    {
        private static string RepoRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "Assets", "Tests", "EditMode", "CanSuyu")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        [Test]
        public void Lint_GateTests_MeasureTrajectories_NotFixedHourFrames()
        {
            var root = RepoRoot();
            if (root == null)
            {
                Assert.Inconclusive("repo root not reachable from test runner cwd — lint skipped");
                return;
            }

            // Comments may TALK about screenshots (that is the contract's own language);
            // only CODE lines are linted.
            var source = string.Join("\n", File.ReadAllLines(Path.Combine(
                    root, "Assets", "Tests", "EditMode", "CanSuyu", "LivingWorldGateTests.cs"))
                .Where(line => !line.TrimStart().StartsWith("//")));

            // Every gate must ADVANCE the world through time (a loop of ticks), and the file
            // may not smuggle in the old proof style: single-frame screenshots or a lone
            // hardcoded clock check standing in for behavior.
            var gateBodies = source.Split(new[] { "[Test]" }, System.StringSplitOptions.None).Skip(1).ToArray();
            Assert.That(gateBodies.Length, Is.GreaterThanOrEqualTo(6), "gates went missing");
            foreach (var body in gateBodies)
            {
                bool advances = body.Contains("AdvanceDays(") || body.Contains("composer.Advance(");
                Assert.That(advances, Is.True,
                    "a gate test does not advance world time — it can only be checking a staged frame");
            }

            string[] banned = { "Screenshot", "screenshot", "CaptureFrame", "IsAtHour(12" };
            foreach (var token in banned)
                Assert.That(source.Contains(token), Is.False,
                    $"gate file contains '{token}' — frames are supportive evidence, never a gate");
        }

        [Test]
        public void Lint_RoadmapDoDs_DoNotPromiseFixedHourOrScreenshotProofs()
        {
            var root = RepoRoot();
            if (root == null)
            {
                Assert.Inconclusive("repo root not reachable from test runner cwd — lint skipped");
                return;
            }

            var roadmap = File.ReadAllLines(Path.Combine(root, "docs", "ROADMAP_V2_CAN_SUYU.md"));
            string[] bannedPhrases = { "sabit saat", "ekran görüntüsü yeterli", "screenshot proof", "saat 12'de" };
            var offending = roadmap
                .Where(line => line.Contains("DoD:"))
                .Where(line => bannedPhrases.Any(p => line.ToLowerInvariant().Contains(p.ToLowerInvariant())))
                .ToArray();
            Assert.That(offending, Is.Empty,
                "a roadmap DoD promises a fixed-hour/screenshot proof — the V1 rot, rejected by contract: "
                + string.Join(" | ", offending));
        }
    }
}

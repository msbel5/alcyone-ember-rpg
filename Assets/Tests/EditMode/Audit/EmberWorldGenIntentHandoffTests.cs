using EmberCrpg.Presentation.Ember.UI;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Ninth-pass audit (G-P2): pins the scene-handoff contract that
    /// EmberWorldGenUI.BeginJourney and EmberWorldHost.Awake split between
    /// them.
    ///
    /// EmberWorldHost is a MonoBehaviour and cannot be constructed in the
    /// fallback harness, so this test exercises the contract directly:
    ///   1. EmberWorldGenUI sets EmberWorldGenIntent.Pending with the
    ///      player's mood/calling/start.
    ///   2. The next scene's EmberWorldHost.Awake reads Pending, forwards
    ///      mood/calling/start to its IPlayerCommandSink.SeedWorld(...), and
    ///      clears Pending so a later scene re-load does not re-seed.
    ///
    /// We replicate step 2 here with a recording sink so the contract is
    /// pinned even when the real Unity adapter cannot be constructed.
    /// </summary>
    public sealed class EmberWorldGenIntentHandoffTests
    {
        private sealed class RecordingSeedSink
        {
            public int SeedCallCount { get; private set; }
            public string LastMood { get; private set; }
            public string LastCalling { get; private set; }
            public string LastStart { get; private set; }

            public void SeedWorld(string mood, string calling, string startLocation)
            {
                SeedCallCount++;
                LastMood = mood;
                LastCalling = calling;
                LastStart = startLocation;
            }
        }

        // Mirrors the actual consumption block in
        // Assets/Scripts/Presentation/Ember/Bootstrap/EmberWorldHost.cs:79
        // Future changes to that block must be reflected here.
        private static void HostAwakeConsumePendingIntent(RecordingSeedSink commands)
        {
            var pending = EmberWorldGenIntent.Pending;
            if (pending != null && !pending.IsEmpty)
            {
                commands?.SeedWorld(pending.Mood, pending.Calling, pending.Start);
                EmberWorldGenIntent.Pending = null;
            }
        }

        [SetUp]
        public void ResetPending()
        {
            // Static state — clear before every test.
            EmberWorldGenIntent.Pending = null;
        }

        [TearDown]
        public void ClearPendingAfter()
        {
            EmberWorldGenIntent.Pending = null;
        }

        [Test]
        public void HandoffContract_PendingIntent_IsForwardedToSeedWorldAndCleared()
        {
            // 1. UI layer sets Pending with the wizard answers.
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent(
                mood: "stoic",
                calling: "smith",
                start: "Hearthhold");

            Assert.That(EmberWorldGenIntent.Pending, Is.Not.Null,
                "Sanity: Pending must be set before host Awake runs.");

            // 2. The fresh world-host's Awake consumes Pending exactly once.
            var sink = new RecordingSeedSink();
            HostAwakeConsumePendingIntent(sink);

            // SeedWorld received the exact same values.
            Assert.That(sink.SeedCallCount, Is.EqualTo(1),
                "EmberWorldHost.Awake must call SeedWorld exactly once when Pending is set.");
            Assert.That(sink.LastMood,    Is.EqualTo("stoic"));
            Assert.That(sink.LastCalling, Is.EqualTo("smith"));
            Assert.That(sink.LastStart,   Is.EqualTo("Hearthhold"));

            // Pending is cleared so a subsequent scene re-load does not re-seed.
            Assert.That(EmberWorldGenIntent.Pending, Is.Null,
                "Pending must be cleared after consumption.");
        }

        [Test]
        public void HandoffContract_NoPendingIntent_LeavesSeedWorldUntouched()
        {
            EmberWorldGenIntent.Pending = null;

            var sink = new RecordingSeedSink();
            HostAwakeConsumePendingIntent(sink);

            Assert.That(sink.SeedCallCount, Is.EqualTo(0),
                "When Pending is null the host must NOT call SeedWorld.");
            Assert.That(EmberWorldGenIntent.Pending, Is.Null);
        }

        [Test]
        public void HandoffContract_EmptyIntent_IsTreatedAsNoOp()
        {
            // The wizard may have been entered and left blank.
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent(
                mood: string.Empty,
                calling: string.Empty,
                start: string.Empty);

            Assert.That(EmberWorldGenIntent.Pending.IsEmpty, Is.True,
                "Sanity: an all-empty intent must report IsEmpty=true.");

            var sink = new RecordingSeedSink();
            HostAwakeConsumePendingIntent(sink);

            Assert.That(sink.SeedCallCount, Is.EqualTo(0),
                "An empty Pending intent must be treated as no-op by the host.");
            // The contract chooses NOT to clear an empty pending intent — the
            // production code's `if (pending != null && !pending.IsEmpty)`
            // guard short-circuits before the clear, so we mirror that.
            Assert.That(EmberWorldGenIntent.Pending, Is.Not.Null,
                "Empty Pending must remain as-is — only consumption clears it.");
        }

        [Test]
        public void HandoffContract_SecondAwakeAfterConsumption_IsNoOp()
        {
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent(
                mood: "curious",
                calling: "scholar",
                start: "Library");

            var sink = new RecordingSeedSink();
            HostAwakeConsumePendingIntent(sink);
            Assert.That(sink.SeedCallCount, Is.EqualTo(1));

            // Simulate a second EmberWorldHost.Awake (additive scene load).
            // SeedWorld must NOT fire a second time because Pending was cleared.
            HostAwakeConsumePendingIntent(sink);
            Assert.That(sink.SeedCallCount, Is.EqualTo(1),
                "After consumption Pending is null — a second Awake must be a no-op.");
        }

        [Test]
        public void Intent_NullStrings_AreNormalisedToEmpty()
        {
            // Defensive: the wizard inputs may be null when the TMP_InputField
            // ref itself is null. The intent ctor must normalise to empty so
            // SeedWorld never sees null.
            var intent = new EmberWorldGenIntent(null, null, null);
            Assert.That(intent.Mood,    Is.EqualTo(string.Empty));
            Assert.That(intent.Calling, Is.EqualTo(string.Empty));
            Assert.That(intent.Start,   Is.EqualTo(string.Empty));
            Assert.That(intent.IsEmpty, Is.True);
        }

        [Test]
        public void Intent_WithCharacter_CarriesClassBirthsignNameAndAnswers()
        {
            var intent = new EmberWorldGenIntent("grim", "mage", "capital")
                .WithCharacter("Mami", "mage", "the_lover", new[] { "a", "c" });

            Assert.That(intent.Mood, Is.EqualTo("grim"));
            Assert.That(intent.PlayerName, Is.EqualTo("Mami"));
            Assert.That(intent.CharacterClassId, Is.EqualTo("mage"));
            Assert.That(intent.BirthsignId, Is.EqualTo("the_lover"));
            Assert.That(intent.AnswerChoiceIds, Is.EqualTo(new[] { "a", "c" }));
            Assert.That(intent.IsEmpty, Is.False);
        }
    }
}

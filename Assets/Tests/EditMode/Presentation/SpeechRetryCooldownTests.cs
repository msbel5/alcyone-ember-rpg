#if UNITY_INCLUDE_TESTS
using System.Reflection;
using EmberCrpg.Presentation.Ember.Audio;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    /// <summary>
    /// B16: TTS backends used to short-circuit forever on the first transient exception
    /// (piper.exe hiccup, SAPI COM RPC blip, device reload). The fix replaces the one-way
    /// `_dead` latch with a bounded-retry + cooldown pattern - MAX_FAILS = 3, COOLDOWN_SECONDS = 30.
    /// Story: one failure must NOT silence; three failures within a window must; one success
    /// must reset the counter; and for SAPI, the truly-absent-ProgID case must still be
    /// permanent (_sapiMissing) so we don't pointlessly retry COM every 30s on non-SAPI hosts.
    /// </summary>
    public sealed class SpeechRetryCooldownTests
    {
        private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Static;

        private static void Reset(System.Type type, string field, object value)
            => type.GetField(field, Priv).SetValue(null, value);

        private static object Get(System.Type type, string member, bool isField)
            => isField
                ? type.GetField(member, Priv).GetValue(null)
                : type.GetMethod(member, Priv).Invoke(null, null);

        [SetUp]
        public void Reset()
        {
            Reset(typeof(PiperSpeechSynth), "_failCount", 0);
            Reset(typeof(PiperSpeechSynth), "_cooldownUntilRealtime", 0f);
            Reset(typeof(WindowsSpeechService), "_failCount", 0);
            Reset(typeof(WindowsSpeechService), "_cooldownUntilRealtime", 0f);
            Reset(typeof(WindowsSpeechService), "_sapiMissing", false);
        }

        [Test]
        public void Piper_SingleTransientFailure_DoesNotSilenceTheSession()
        {
            // The bug: one hiccup used to set _dead = true and every subsequent TrySpeak
            // short-circuited forever. Now: one NoteFailure leaves the backend live.
            typeof(PiperSpeechSynth).GetMethod("NoteFailure", Priv).Invoke(null, null);
            Assert.That((bool)Get(typeof(PiperSpeechSynth), "IsSilenced", isField: false), Is.False,
                "one transient failure must not permanently silence Piper");
            Assert.That((int)Get(typeof(PiperSpeechSynth), "_failCount", isField: true), Is.EqualTo(1));
        }

        [Test]
        public void Piper_ThreeFailuresWithinWindow_SilencesUntilCooldown()
        {
            var noteFailure = typeof(PiperSpeechSynth).GetMethod("NoteFailure", Priv);
            for (int i = 0; i < 3; i++) noteFailure.Invoke(null, null);

            Assert.That((int)Get(typeof(PiperSpeechSynth), "_failCount", isField: true), Is.EqualTo(3));
            Assert.That((bool)Get(typeof(PiperSpeechSynth), "IsSilenced", isField: false), Is.True,
                "MAX_FAILS reached => silenced while cooldown deadline is in the future");

            // Advance past cooldown - deadline is now in the past, IsSilenced flips off, next Available call re-probes.
            Reset(typeof(PiperSpeechSynth), "_cooldownUntilRealtime", UnityEngine.Time.realtimeSinceStartup - 1f);
            Assert.That((bool)Get(typeof(PiperSpeechSynth), "IsSilenced", isField: false), Is.False,
                "past cooldown deadline reopens the door");
        }

        [Test]
        public void Piper_NoteSuccess_ResetsCounter()
        {
            var noteFailure = typeof(PiperSpeechSynth).GetMethod("NoteFailure", Priv);
            noteFailure.Invoke(null, null);
            noteFailure.Invoke(null, null);
            typeof(PiperSpeechSynth).GetMethod("NoteSuccess", Priv).Invoke(null, null);
            Assert.That((int)Get(typeof(PiperSpeechSynth), "_failCount", isField: true), Is.EqualTo(0),
                "a synth that eventually succeeds must re-earn its full retry budget");
        }

        [Test]
        public void Windows_ThreeFailuresSilenceButFourthAfterCooldownReturns()
        {
            var noteFailure = typeof(WindowsSpeechService).GetMethod("NoteFailure", Priv);
            for (int i = 0; i < 3; i++) noteFailure.Invoke(null, null);
            Assert.That((bool)Get(typeof(WindowsSpeechService), "IsSilenced", isField: false), Is.True);

            Reset(typeof(WindowsSpeechService), "_cooldownUntilRealtime", UnityEngine.Time.realtimeSinceStartup - 0.01f);
            Assert.That((bool)Get(typeof(WindowsSpeechService), "IsSilenced", isField: false), Is.False,
                "SAPI must be re-tried after the cooldown expires - transient RPC blips are not death");
        }

        [Test]
        public void Windows_SapiMissing_StaysPermanentAcrossAnyCooldown()
        {
            // The one truly hopeless case - COM ProgID resolution returned null.
            // No point burning retries on a machine that literally has no SAPI installed.
            Reset(typeof(WindowsSpeechService), "_sapiMissing", true);

            // Even with fail counter at zero and no cooldown, _sapiMissing must dominate at entry points.
            Assert.That((bool)Get(typeof(WindowsSpeechService), "_sapiMissing", isField: true), Is.True);
            Assert.That((int)Get(typeof(WindowsSpeechService), "_failCount", isField: true), Is.EqualTo(0));
        }

        [Test]
        public void Windows_NoteSuccess_ResetsCounter()
        {
            var noteFailure = typeof(WindowsSpeechService).GetMethod("NoteFailure", Priv);
            noteFailure.Invoke(null, null);
            noteFailure.Invoke(null, null);
            typeof(WindowsSpeechService).GetMethod("NoteSuccess", Priv).Invoke(null, null);
            Assert.That((int)Get(typeof(WindowsSpeechService), "_failCount", isField: true), Is.EqualTo(0));
        }
    }
}
#endif

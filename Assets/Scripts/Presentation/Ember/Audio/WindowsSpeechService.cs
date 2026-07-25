using System;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Audio
{
    /// <summary>
    /// M3b backend v1: offline Windows SAPI via COM reflection - no package, no network, Mono
    /// only. Now signature-aware: voice picked from the installed roster, rate set per NPC and
    /// pitch woven in as SAPI XML, so every NPC keeps a recognisable voice. Async without purge
    /// so streamed sentences QUEUE naturally; purge only when the speaker changes.
    /// Fails silent-and-once when SAPI is missing. Neural (Piper ONNX) slots in behind
    /// SpeakChunk once a phonemizer lands - callers never change.
    /// </summary>
    public static class WindowsSpeechService
    {
        private static object _voice;
        private static object[] _roster;
        // B16: bounded-retry + cooldown replaces permanent kill. _sapiMissing stays permanent
        // only for the one truly hopeless case: ProgID resolution returning null (SAPI absent).
        private static int _failCount;
        private static float _cooldownUntilRealtime;
        private const int MAX_FAILS = 3;
        private const float COOLDOWN_SECONDS = 30f;
        private static bool _sapiMissing;
        private static bool IsSilenced() => _failCount >= MAX_FAILS && Time.realtimeSinceStartup < _cooldownUntilRealtime;
        private static void NoteFailure() { _failCount++; if (_failCount >= MAX_FAILS) _cooldownUntilRealtime = Time.realtimeSinceStartup + COOLDOWN_SECONDS; }
        private static void NoteSuccess() { if (_failCount != 0) _failCount = 0; }
        private static string _last;

        public static int VoiceCount { get { EnsureVoice(); return _roster?.Length ?? 1; } }

        /// <summary>Legacy single-line entry (proofs, notifications): default signature.</summary>
        public static void Speak(string line)
        {
            if (_sapiMissing || IsSilenced() || string.IsNullOrWhiteSpace(line) || line == _last) return;
            _last = line;
            SpeakChunk(line, new EmberCrpg.Simulation.AiDm.NpcVoiceSignature(0, 1, 0), purgeFirst: true);
        }

        public static void SpeakChunk(string text, EmberCrpg.Simulation.AiDm.NpcVoiceSignature signature, bool purgeFirst)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_sapiMissing || IsSilenced() || string.IsNullOrWhiteSpace(text)) return;
            try
            {
                EnsureVoice();
                if (_voice == null) return;
                var type = _voice.GetType();
                if (_roster != null && _roster.Length > 0)
                {
                    var pick = _roster[((signature.VoiceIndex % _roster.Length) + _roster.Length) % _roster.Length];
                    type.InvokeMember("Voice", System.Reflection.BindingFlags.SetProperty, null, _voice, new[] { pick });
                }
                int rate = Mathf.Clamp(1 + signature.RateOffset, -10, 10);
                type.InvokeMember("Rate", System.Reflection.BindingFlags.SetProperty, null, _voice, new object[] { rate });
                string clipped = text.Length > 300 ? text.Substring(0, 300) : text;
                string xml = $"<pitch absmiddle=\"{Mathf.Clamp(signature.PitchOffset, -10, 10)}\"/>{System.Security.SecurityElement.Escape(clipped)}";
                // 1=async, 8=XML; +2 purge only when the speaker changes mid-utterance.
                int flags = 1 | 8 | (purgeFirst ? 2 : 0);
                type.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, _voice, new object[] { xml, flags });
                NoteSuccess();
            }
            catch (Exception e)
            {
                NoteFailure();
                _voice = null;  // force EnsureVoice re-init on next attempt
                _roster = null;
                Debug.Log($"[Speech] SAPI hiccup ({_failCount}/{MAX_FAILS}), staying silent briefly: {e.Message}");
            }
#endif
        }

        public static void StopSpeaking()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_sapiMissing || IsSilenced() || _voice == null) return;
            try
            {
                // Purge by speaking empty with SVSFPurgeBeforeSpeak - cheapest queue flush SAPI offers.
                _voice.GetType().InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod,
                    null, _voice, new object[] { string.Empty, 1 | 2 });
            }
            catch (Exception) { /* flushing a dead voice is not an error */ }
#endif
        }

        private static void EnsureVoice()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_sapiMissing || IsSilenced() || _voice != null) return;
            try
            {
                var type = Type.GetTypeFromProgID("SAPI.SpVoice");
                // Genuinely absent SAPI (no COM registration) is the one truly permanent case - no point retrying.
                if (type == null) { _sapiMissing = true; return; }
                _voice = Activator.CreateInstance(type);
                var tokens = type.InvokeMember("GetVoices", System.Reflection.BindingFlags.InvokeMethod,
                    null, _voice, new object[] { string.Empty, string.Empty });
                int count = (int)tokens.GetType().InvokeMember("Count",
                    System.Reflection.BindingFlags.GetProperty, null, tokens, null);
                _roster = new object[Math.Max(1, count)];
                for (int i = 0; i < count; i++)
                    _roster[i] = tokens.GetType().InvokeMember("Item",
                        System.Reflection.BindingFlags.InvokeMethod, null, tokens, new object[] { i });
                Debug.Log($"[Speech] SAPI roster: {count} voice(s) - signatures map across them.");
                NoteSuccess();
            }
            catch (Exception e)
            {
                NoteFailure();
                _voice = null;
                _roster = null;
                Debug.Log($"[Speech] SAPI init hiccup ({_failCount}/{MAX_FAILS}), staying silent briefly: {e.Message}");
            }
#endif
        }
    }
}

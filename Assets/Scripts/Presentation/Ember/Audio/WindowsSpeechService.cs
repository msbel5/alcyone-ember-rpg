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
        private static bool _dead;
        private static string _last;

        public static int VoiceCount { get { EnsureVoice(); return _roster?.Length ?? 1; } }

        /// <summary>Legacy single-line entry (proofs, notifications): default signature.</summary>
        public static void Speak(string line)
        {
            if (_dead || string.IsNullOrWhiteSpace(line) || line == _last) return;
            _last = line;
            SpeakChunk(line, new EmberCrpg.Simulation.AiDm.NpcVoiceSignature(0, 1, 0), purgeFirst: true);
        }

        public static void SpeakChunk(string text, EmberCrpg.Simulation.AiDm.NpcVoiceSignature signature, bool purgeFirst)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_dead || string.IsNullOrWhiteSpace(text)) return;
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
            }
            catch (Exception e)
            {
                _dead = true;
                Debug.Log($"[Speech] SAPI unavailable, staying silent: {e.Message}");
            }
#endif
        }

        public static void StopSpeaking()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_dead || _voice == null) return;
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
            if (_dead || _voice != null) return;
            try
            {
                var type = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (type == null) { _dead = true; return; }
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
            }
            catch (Exception e)
            {
                _dead = true;
                Debug.Log($"[Speech] SAPI unavailable, staying silent: {e.Message}");
            }
#endif
        }
    }
}

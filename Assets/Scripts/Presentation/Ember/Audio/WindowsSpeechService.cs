using System;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Audio
{
    /// <summary>
    /// PLAYTEST FIX ("tts yok"): NPC lines are SPOKEN through the offline Windows SAPI voice.
    /// COM by reflection - no package, no network, Mono scripting backend (ProjectSettings
    /// scriptingBackend Standalone: 0). Async with purge: a new line interrupts the old one.
    /// Fails silent-and-once when SAPI is missing (non-Windows or stripped COM).
    /// </summary>
    public static class WindowsSpeechService
    {
        private static object _voice;
        private static bool _dead;
        private static string _last;

        public static void Speak(string line)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_dead || string.IsNullOrWhiteSpace(line) || line == _last) return;
            _last = line;
            try
            {
                if (_voice == null)
                {
                    var type = Type.GetTypeFromProgID("SAPI.SpVoice");
                    if (type == null) { _dead = true; return; }
                    _voice = Activator.CreateInstance(type);
                    type.InvokeMember("Rate", System.Reflection.BindingFlags.SetProperty,
                        null, _voice, new object[] { 1 });
                }
                string clipped = line.Length > 220 ? line.Substring(0, 220) : line;
                // SVSFlagsAsync(1) | SVSFPurgeBeforeSpeak(2): never blocks the frame.
                _voice.GetType().InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod,
                    null, _voice, new object[] { clipped, 3 });
                Debug.Log($"[Speech] speaking {clipped.Length} chars.");
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

using System;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Audio
{
    /// <summary>
    /// M3b: NPC speech with a per-actor VOICE SIGNATURE (deterministic voice/rate/pitch from the
    /// actor id) and SENTENCE STREAMING - completed sentences of a still-generating reply are
    /// spoken while the rest is still decoding, so the ear tracks the stream the eye reads.
    /// Backend today: offline Windows SAPI (roster = installed voices). The neural seam:
    /// StreamingAssets/Models/tts/voice.onnx + a phonemizer will slot in behind SpeakChunk
    /// without touching any caller (onnxruntime already ships for the forge embeddings).
    /// </summary>
    public static class SpeechDirector
    {
        private static ulong _currentKey;
        private static int _spokenChars;
        private static string _lastFinal;

        public static void FeedPartial(ulong voiceKey, string displayLine)
        {
            if (string.IsNullOrWhiteSpace(displayLine)) return;
            var text = StripDisplaySuffix(displayLine);
            if (IsPlaceholder(text)) return;
            RetargetIfNeeded(voiceKey);
            var sentences = EmberCrpg.Simulation.AiDm.SpeechSentenceChunker.Drain(text, ref _spokenChars);
            foreach (var sentence in sentences)
                SpeakRouted(sentence, voiceKey, purgeFirst: false);
        }

        public static void FeedFinal(ulong voiceKey, string finalLine)
        {
            if (string.IsNullOrWhiteSpace(finalLine) || finalLine == _lastFinal) return;
            _lastFinal = finalLine;
            RetargetIfNeeded(voiceKey);
            // Speak whatever the stream had not finished when generation ended (usually the
            // last clause without a terminator). If the final text diverged from what we
            // spoke (sanitizers rewrote it), start clean rather than glue mismatched halves.
            if (_spokenChars > finalLine.Length) { _spokenChars = 0; }
            var remainder = finalLine.Substring(Math.Min(_spokenChars, finalLine.Length)).Trim();
            _spokenChars = finalLine.Length;
            if (remainder.Length > 1)
                SpeakRouted(remainder, voiceKey, purgeFirst: _spokenChars == remainder.Length);
        }

        // M3b.2: neural first - 904 LibriTTS speakers via piper; SAPI keeps duty when the
        // voice model is not shipped or the process dies. Same signature maths either way.
        private static void SpeakRouted(string text, ulong voiceKey, bool purgeFirst)
        {
            if (PiperSpeechSynth.Available)
            {
                var neural = EmberCrpg.Simulation.AiDm.NpcVoiceSignatureService.SignatureFor(
                    voiceKey, PiperSpeechSynth.NumSpeakers);
                if (purgeFirst) SpeechPlaybackHost.Flush();
                if (PiperSpeechSynth.TrySpeak(text, neural.VoiceIndex, out var wavPath))
                {
                    SpeechPlaybackHost.Enqueue(wavPath, 1f + neural.PitchOffset * 0.015f);
                    return;
                }
            }
            WindowsSpeechService.SpeakChunk(text, SignatureFor(voiceKey), purgeFirst);
        }

        private static EmberCrpg.Simulation.AiDm.NpcVoiceSignature SignatureFor(ulong voiceKey)
            => EmberCrpg.Simulation.AiDm.NpcVoiceSignatureService.SignatureFor(
                voiceKey, WindowsSpeechService.VoiceCount);

        private static void RetargetIfNeeded(ulong voiceKey)
        {
            if (voiceKey == _currentKey) return;
            _currentKey = voiceKey;
            _spokenChars = 0;
            _lastFinal = null;
            WindowsSpeechService.StopSpeaking(); // a new speaker never finishes the old one's line
            SpeechPlaybackHost.Flush();
        }

        private static string StripDisplaySuffix(string line)
            => line.EndsWith(" …", StringComparison.Ordinal) ? line.Substring(0, line.Length - 2) : line;

        private static bool IsPlaceholder(string line)
            => line == "Thinking…" || line.EndsWith(" thinks…", StringComparison.Ordinal) || line == "...";
    }
}

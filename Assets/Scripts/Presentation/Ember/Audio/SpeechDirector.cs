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
        private static string _streamPrefix = string.Empty;
        private static ulong _anchorKey;
        private static Transform _anchorTransform;

        /// <summary>Positional voice: the conversation NPC (matched by voiceKey) speaks FROM its
        /// billboard; every other key (player voice, oracle 7, narration) stays 2D by construction.</summary>
        public static void SetSpeakerAnchor(ulong voiceKey, Transform anchor)
        {
            _anchorKey = voiceKey;
            _anchorTransform = anchor;
        }

        /// <summary>LIVE BUG ('konusmadan cikinca tts durmuyor'): the conversation is OVER - cut
        /// the whole queue (piper + SAPI) and reset stream state. RetargetIfNeeded keeps its
        /// queue-on-retarget semantics; this fires only from the dialog close paths.</summary>
        public static void StopConversationSpeech()
        {
            _currentKey = 0; _spokenChars = 0; _lastFinal = null; _streamPrefix = string.Empty;
            _anchorKey = 0; _anchorTransform = null;
            SpeechPlaybackHost.Flush();
            WindowsSpeechService.StopSpeaking();
        }

        public static void FeedPartial(ulong voiceKey, string displayLine)
        {
            if (string.IsNullOrWhiteSpace(displayLine)) return;
            var text = StripDisplaySuffix(displayLine);
            if (IsPlaceholder(text)) return;
            RetargetIfNeeded(voiceKey);
            // LIVE BUG ("ilk cumleyi seslendirmedi"): a SECOND question to the SAME speaker kept
            // the previous answer's spoken-offset, so the new answer's first sentence fell below
            // it and was never voiced. A shrinking or diverging stream = a new answer: reset.
            bool newStream = text.Length < _spokenChars
                || (_streamPrefix.Length > 0
                    && !text.StartsWith(_streamPrefix, StringComparison.Ordinal));
            if (newStream) { _spokenChars = 0; _streamPrefix = string.Empty; }
            if (_streamPrefix.Length == 0 && text.Length >= 12) _streamPrefix = text.Substring(0, 12);
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
            _streamPrefix = string.Empty; // the next stream re-anchors itself
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
                    SpeechPlaybackHost.Enqueue(wavPath, 1f + neural.PitchOffset * 0.015f,
                        voiceKey == _anchorKey ? _anchorTransform : null);
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
            // LIVE BUG ('sectigimizi seslendirmedi', 'cumlenin ortasinda basladi'): flushing on
            // speaker change cut the player's just-spoken question the moment the NPC stream
            // began. Voices now QUEUE in conversation order; only a same-speaker replacement purges.
        }

        private static string StripDisplaySuffix(string line)
            => line.EndsWith(" …", StringComparison.Ordinal) ? line.Substring(0, line.Length - 2) : line;

        private static bool IsPlaceholder(string line)
            => line == "Thinking…" || line.EndsWith(" thinks…", StringComparison.Ordinal) || line == "...";
    }
}

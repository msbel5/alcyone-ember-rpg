namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>M3b: one NPC, one voice - forever. Derived from the actor id the way the forge
    /// derives a portrait from its seed, so the ear learns who speaks before the eye reads.</summary>
    public readonly struct NpcVoiceSignature
    {
        /// <summary>Index into whatever voice roster the backend has (modulo-mapped).</summary>
        public readonly int VoiceIndex;
        /// <summary>Speaking-rate offset, -3..+3 backend units.</summary>
        public readonly int RateOffset;
        /// <summary>Pitch offset, -9..+9 backend units.</summary>
        public readonly int PitchOffset;

        public NpcVoiceSignature(int voiceIndex, int rateOffset, int pitchOffset)
        {
            VoiceIndex = voiceIndex;
            RateOffset = rateOffset;
            PitchOffset = pitchOffset;
        }
    }

    public static class NpcVoiceSignatureService
    {
        /// <summary>Stable signature for a voice key; availableVoices maps VoiceIndex into the
        /// installed roster (SAPI today, a multi-speaker neural voice tomorrow).</summary>
        public static NpcVoiceSignature SignatureFor(ulong voiceKey, int availableVoices)
        {
            ulong h = voiceKey + 0x9E3779B97F4A7C15UL;
            h ^= h >> 30; h *= 0xBF58476D1CE4E5B9UL;
            h ^= h >> 27; h *= 0x94D049BB133111EBUL;
            h ^= h >> 31;
            int voices = availableVoices > 0 ? availableVoices : 1;
            int voiceIndex = (int)(h % (ulong)voices);
            int rate = (int)((h >> 8) % 7UL) - 3;    // -3..+3
            int pitch = (int)((h >> 16) % 19UL) - 9; // -9..+9
            return new NpcVoiceSignature(voiceIndex, rate, pitch);
        }

        /// <summary>FNV-1a for authored actors that only have a name - stable across sessions.</summary>
        public static ulong VoiceKeyFor(string actorName)
        {
            ulong hash = 1469598103934665603UL;
            foreach (var ch in actorName ?? string.Empty) { hash ^= ch; hash *= 1099511628211UL; }
            return hash == 0UL ? 1UL : hash;
        }
    }

    /// <summary>M3b: pure sentence drain for streamed speech - complete sentences leave the
    /// buffer as they finish forming; the tail stays until its terminator arrives.</summary>
    public static class SpeechSentenceChunker
    {
        public static System.Collections.Generic.List<string> Drain(string text, ref int fromIndex)
        {
            var complete = new System.Collections.Generic.List<string>();
            if (string.IsNullOrEmpty(text) || fromIndex >= text.Length) return complete;
            int start = fromIndex < 0 ? 0 : fromIndex;
            for (int p = start; p < text.Length; p++)
            {
                char c = text[p];
                if (c != '.' && c != '!' && c != '?') continue;
                int end = p + 1;
                var chunk = text.Substring(start, end - start).Trim();
                if (chunk.Length > 1) complete.Add(chunk);
                start = end;
            }
            fromIndex = start;
            return complete;
        }
    }
}

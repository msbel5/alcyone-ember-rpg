namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// M3b.3 ("bizim de sesimiz sorularimizdan olussun"): the PLAYER's voice signature derives
    /// from who you made at creation - name + class - exactly how the forge derives a portrait
    /// from its seed. Both fields persist in saves, so the voice survives every reload.
    /// </summary>
    public static class PlayerVoiceService
    {
        public static ulong PlayerVoiceKey(string playerName, string className)
        {
            ulong key = NpcVoiceSignatureService.VoiceKeyFor(playerName);
            key ^= NpcVoiceSignatureService.VoiceKeyFor(className) * 0x9E3779B97F4A7C15UL;
            return key == 0UL ? 2UL : key;
        }
    }
}

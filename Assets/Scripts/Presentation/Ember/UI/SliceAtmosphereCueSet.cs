// Design note:
// SliceAtmosphereCueSet is the presentation-facing contract for audio and atmosphere hooks.
// Inputs: deterministic cue ids chosen from the current slice world state.
// Outputs: debug-visible ambience/music/SFX ids that optional Unity audio drivers can consume.
// Bible reference: Sprint 4 Phase 5 audio/atmosphere hooks without simulation audio coupling.
namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>Small immutable bundle of current atmosphere cue ids.</summary>
    public sealed class SliceAtmosphereCueSet
    {
        public SliceAtmosphereCueSet(string ambienceId, string musicId, string sfxId, string reason)
        {
            AmbienceId = ambienceId;
            MusicId = musicId;
            SfxId = sfxId;
            Reason = reason;
        }

        public string AmbienceId { get; }
        public string MusicId { get; }
        public string SfxId { get; }
        public string Reason { get; }

        public static SliceAtmosphereCueSet Silent(string reason)
        {
            return new SliceAtmosphereCueSet("ambience.none", "music.none", "sfx.none", reason);
        }
    }
}

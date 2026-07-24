using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Audio
{
    /// <summary>
    /// W31 ('kendi aralarinda konustuklarinda da tts olmali'): nearby NPC exchanges get a SPATIAL
    /// voice. Its own host + AudioSource - never the dialog queue, so conversations and ambient
    /// mutters cannot flush each other. Piper-only by design (SAPI shares one COM queue with
    /// dialog); budget 1 concurrent voice + cooldown keeps the town murmuring, not shouting.
    /// </summary>
    public static class AmbientVoiceDirector
    {
        private static AmbientVoiceHost s_host;
        private static float s_nextOfferTime;

        public static void Offer(ulong actorId, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (!PiperSpeechSynth.Available) return;
            if (Time.unscaledTime < s_nextOfferTime) return;
            if (EmberCrpg.Presentation.Ember.UI.InGame.InGameUiController.AnyScreenOpen) return;
            Ensure();
            if (s_host.Busy) return;

            Transform anchor = null;
            foreach (var view in Object.FindObjectsByType<EmberCrpg.Presentation.Ember.Views.ActorView>(FindObjectsSortMode.None))
            {
                if (!view.HasDomainActorId || view.DomainActorId.Value != actorId) continue;
                anchor = view.transform;
                break;
            }
            var listener = UnityEngine.Camera.main;
            if (anchor == null || listener == null) return;
            if (Vector3.Distance(listener.transform.position, anchor.position) > 18f) return; // out of earshot

            var signature = EmberCrpg.Simulation.AiDm.NpcVoiceSignatureService.SignatureFor(
                actorId, PiperSpeechSynth.NumSpeakers);
            if (!PiperSpeechSynth.TrySpeak(line, signature.VoiceIndex, out var wavPath)) return;
            s_host.Play(wavPath, 1f + signature.PitchOffset * 0.015f, anchor);
            s_nextOfferTime = Time.unscaledTime + 30f;
        }

        private static void Ensure()
        {
            if (s_host != null) return;
            var go = new GameObject("AmbientVoiceHost");
            Object.DontDestroyOnLoad(go);
            s_host = go.AddComponent<AmbientVoiceHost>();
        }
    }

    public sealed class AmbientVoiceHost : MonoBehaviour
    {
        private AudioSource _source;
        private Transform _anchor;
        private string _pendingWav;
        private float _pendingPitch;

        public bool Busy => _pendingWav != null || (_source != null && _source.isPlaying);

        public void Play(string wavPath, float pitch, Transform anchor)
        {
            EnsureSource();
            _pendingWav = wavPath;
            _pendingPitch = pitch;
            _anchor = anchor;
        }

        private void EnsureSource()
        {
            if (_source != null) return;
            _source = gameObject.AddComponent<AudioSource>();
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 2f;
            _source.maxDistance = 18f;
            _source.dopplerLevel = 0f;
            _source.volume = 0.75f; // a mutter across the street, not a stage line
        }

        private void Update()
        {
            if (_anchor != null) transform.position = _anchor.position;
            if (_pendingWav == null || _source == null || _source.isPlaying) return;
            var clip = SpeechPlaybackHost.TryLoadFinishedWavPublic(_pendingWav);
            if (clip == null) return; // piper still writing
            _pendingWav = null;
            _source.pitch = _pendingPitch;
            _source.clip = clip;
            _source.Play();
        }
    }
}

using System;
using System.Linq;
using UnityEngine;

// Design note:
// SliceAudioCueDriver is an optional Unity playback adapter for presentation cue ids.
// Inputs: SliceAtmosphereCueSet from the session/HUD layer plus inspector-assigned clip bindings.
// Outputs: safe AudioSource playback when clips exist; no external assets are required.
// Bible reference: Sprint 4 Faz 5 optional audio hooks without domain/simulation UnityEngine references.
namespace EmberCrpg.Presentation.Slice
{
    /// <summary>Optional AudioSource adapter; missing sources or clips degrade to silent debug hooks.</summary>
    public sealed class SliceAudioCueDriver : MonoBehaviour
    {
        [Serializable]
        public sealed class ClipBinding
        {
            public string cueId;
            public AudioClip clip;
        }

        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private ClipBinding[] clips = Array.Empty<ClipBinding>();

        private string _lastAmbience;
        private string _lastMusic;
        private string _lastSfx;

        public void Apply(SliceAtmosphereCueSet cues)
        {
            if (cues == null)
                return;

            if (_lastAmbience != cues.AmbienceId)
            {
                PlayLoop(ambienceSource, FindClip(cues.AmbienceId));
                _lastAmbience = cues.AmbienceId;
            }

            if (_lastMusic != cues.MusicId)
            {
                PlayLoop(musicSource, FindClip(cues.MusicId));
                _lastMusic = cues.MusicId;
            }

            if (_lastSfx != cues.SfxId)
            {
                PlayOneShot(sfxSource, FindClip(cues.SfxId));
                _lastSfx = cues.SfxId;
            }
        }

        private AudioClip FindClip(string cueId)
        {
            return clips.FirstOrDefault(binding => binding != null && binding.cueId == cueId)?.clip;
        }

        private static void PlayLoop(AudioSource source, AudioClip clip)
        {
            if (source == null)
                return;
            if (clip == null)
            {
                source.Stop();
                source.clip = null;
                return;
            }

            source.loop = true;
            source.clip = clip;
            source.Play();
        }

        private static void PlayOneShot(AudioSource source, AudioClip clip)
        {
            if (source != null && clip != null)
                source.PlayOneShot(clip);
        }
    }
}

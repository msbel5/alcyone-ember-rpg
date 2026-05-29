using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Audio
{
    public sealed class EmberAmbientAudio : MonoBehaviour
    {
        public enum AmbientType { Outdoors, Indoors }
        [SerializeField] private AmbientType _type;

        private AudioSource _source;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = true;
            _source.volume = 0.12f;
            _source.clip = CreateAmbientBed(_type == AmbientType.Indoors);
            _source.Play();
        }

        // A pure sine wave reads as a tuning-fork "ringing" (the çınlama the player heard). The
        // ambient bed is now soft low-passed noise — a gentle wind / room-tone rumble with no
        // tonal pitch, so it never rings. Indoors is darker (heavier low-pass) than outdoors.
        // Until real ambience assets exist this is a quiet placeholder; set volume to 0 to mute.
        private static AudioClip CreateAmbientBed(bool indoors)
        {
            int sampleRate = 44100;
            int length = sampleRate * 4; // 4s loop
            float[] samples = new float[length];
            float cutoff = indoors ? 0.015f : 0.04f; // smaller = darker rumble
            float prev = 0f;
            for (int i = 0; i < length; i++)
            {
                float white = Random.value * 2f - 1f;
                prev = Mathf.Lerp(prev, white, cutoff); // low-passed white noise = soft wind, no pitch
                samples[i] = Mathf.Clamp(prev * 4f, -1f, 1f);
                // Fade the seam so the 4s loop doesn't click.
                if (i < 2000) samples[i] *= i / 2000f;
                if (i > length - 2000) samples[i] *= (length - i) / 2000f;
            }
            AudioClip clip = AudioClip.Create("AmbientBed", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

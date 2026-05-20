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
            _source.volume = 0.2f;
            _source.clip = CreateSineSweep(_type == AmbientType.Outdoors ? 440f : 220f);
            _source.Play();
        }

        private static AudioClip CreateSineSweep(float frequency)
        {
            int sampleRate = 44100;
            int length = sampleRate * 3;
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                // Subtle modulation to simulate ambient hum/wind
                float mod = Mathf.Sin(t * 0.5f) * 5f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * (frequency + mod) * t) * 0.5f;
                // Fade in/out to loop better (though it's a 3s clip)
                if (i < 1000) samples[i] *= i / 1000f;
                if (i > length - 1000) samples[i] *= (length - i) / 1000f;
            }
            AudioClip clip = AudioClip.Create("AmbientSine", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

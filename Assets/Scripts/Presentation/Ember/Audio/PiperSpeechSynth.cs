using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Audio
{
    /// <summary>
    /// M3b.2: NEURAL local TTS - piper.exe (rhasspy/piper, MIT) kept alive as a child process,
    /// LibriTTS-R medium voice (904 speakers). One JSON line per sentence goes to stdin, one
    /// finished WAV comes back; SpeechPlaybackHost plays them in order with the speaker's
    /// pitch. Process isolation means piper's own onnxruntime NEVER meets the forge's copy.
    /// Boot: ~1-2 s once (model load); warm sentences synthesize at RTF ~0.07.
    /// Missing files = Available false and the SAPI backend keeps talking - no hard dependency.
    /// </summary>
    public static class PiperSpeechSynth
    {
        private const string VoiceFile = "en_US-libritts_r-medium.onnx";
        private static System.Diagnostics.Process _proc;
        private static StreamWriter _stdin;
        private static string _outDir;
        private static int _seq;
        private static int _numSpeakers;
        private static bool _dead;
        private static bool _probed;
        private static string _piperDir;

        public static bool Available
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                if (_dead) return false;
                if (!_probed)
                {
                    _probed = true;
                    var dir = Path.Combine(Application.streamingAssetsPath, "Models", "tts", "piper");
                    if (File.Exists(Path.Combine(dir, "piper.exe")) && File.Exists(Path.Combine(dir, VoiceFile)))
                    {
                        _piperDir = dir;
                        _numSpeakers = ReadNumSpeakers(Path.Combine(dir, VoiceFile + ".json"));
                        Debug.Log($"[Piper] voice found - {_numSpeakers} speakers on the roster.");
                    }
                    else
                    {
                        Debug.Log("[Piper] no voice model shipped - SAPI backend stays on duty.");
                    }
                }
                return _piperDir != null;
#else
                return false;
#endif
            }
        }

        public static int NumSpeakers => _numSpeakers > 0 ? _numSpeakers : 1;

        public static bool TrySpeak(string text, int speakerId, out string wavPath)
        {
            wavPath = null;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!Available) return false;
            try
            {
                EnsureProcess();
                if (_proc == null || _proc.HasExited) { _dead = true; return false; }
                wavPath = Path.Combine(_outDir, $"utt_{++_seq:00000}.wav").Replace('\\', '/');
                string json = "{\"text\":\"" + JsonEscape(text) + "\",\"speaker_id\":" + speakerId
                    + ",\"output_file\":\"" + wavPath + "\"}";
                _stdin.WriteLine(json);
                _stdin.Flush();
                return true;
            }
            catch (Exception e)
            {
                _dead = true;
                Debug.Log($"[Piper] synth failed, falling back to SAPI: {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static void EnsureProcess()
        {
            if (_proc != null && !_proc.HasExited) return;
            _outDir = Path.Combine(Application.temporaryCachePath, "tts-out");
            Directory.CreateDirectory(_outDir);
            foreach (var stale in Directory.GetFiles(_outDir, "utt_*.wav"))
                try { File.Delete(stale); } catch (IOException) { /* still playing from a crash - skip */ }

            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(_piperDir, "piper.exe"),
                Arguments = $"--model \"{Path.Combine(_piperDir, VoiceFile)}\" --json-input",
                WorkingDirectory = _piperDir, // espeak-ng-data resolves relative to here
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            _proc = System.Diagnostics.Process.Start(info);
            _stdin = _proc.StandardInput;
            // Drain both pipes or piper stalls when their buffers fill.
            _proc.OutputDataReceived += (_, __) => { };
            _proc.ErrorDataReceived += (_, __) => { };
            _proc.BeginOutputReadLine();
            _proc.BeginErrorReadLine();
            Application.quitting += Kill;
            Debug.Log("[Piper] process up - neural voices online.");
        }

        private static void Kill()
        {
            try { if (_proc != null && !_proc.HasExited) _proc.Kill(); } catch (Exception) { }
            _proc = null;
        }

        private static int ReadNumSpeakers(string configPath)
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var marker = "\"num_speakers\":";
                int at = json.IndexOf(marker, StringComparison.Ordinal);
                if (at < 0) return 1;
                int start = at + marker.Length;
                int end = start;
                while (end < json.Length && (char.IsDigit(json[end]) || json[end] == ' ')) end++;
                return int.TryParse(json.Substring(start, end - start).Trim(), out var n) && n > 0 ? n : 1;
            }
            catch (Exception) { return 1; }
        }

        private static string JsonEscape(string text)
            => text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
#endif
    }

    /// <summary>
    /// Plays piper's finished WAVs in order on one AudioSource. A speaker change flushes the
    /// queue mid-word. WAVs are gated on an EXCLUSIVE open: while piper still writes, the
    /// open fails and we retry next poll - no half-read clips.
    /// </summary>
    public sealed class SpeechPlaybackHost : MonoBehaviour
    {
        private static SpeechPlaybackHost s_instance;
        private readonly Queue<(string path, float pitch, Transform anchor)> _queue =
            new Queue<(string, float, Transform)>();
        private AudioSource _source;
        private float _nextPoll;
        private Transform _currentAnchor;

        public static void Enqueue(string wavPath, float pitch, Transform anchor = null)
        {
            Ensure();
            s_instance._queue.Enqueue((wavPath, pitch, anchor));
        }

        public static void Flush()
        {
            if (s_instance == null) return;
            s_instance._queue.Clear();
            if (s_instance._source != null) s_instance._source.Stop();
        }

        private static void Ensure()
        {
            if (s_instance != null) return;
            var go = new GameObject("SpeechPlaybackHost");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<SpeechPlaybackHost>();
            s_instance._source = go.AddComponent<AudioSource>();
            s_instance._source.spatialBlend = 0f; // per-clip: 3D when the clip carries an anchor
            s_instance._source.rolloffMode = AudioRolloffMode.Linear;
            s_instance._source.minDistance = 2f;
            s_instance._source.maxDistance = 18f;
            s_instance._source.dopplerLevel = 0f;
        }

        private void Update()
        {
            if (_source == null) return;
            if (_currentAnchor != null && _source.isPlaying)
                transform.position = _currentAnchor.position; // the voice walks with the speaker
            if (_source.isPlaying || _queue.Count == 0) return;
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.10f;

            var (path, pitch, anchor) = _queue.Peek();
            var clip = TryLoadFinishedWav(path);
            if (clip == null) return; // piper still writing - retry on the next poll
            _queue.Dequeue();
            // 'sesler konumsal olmali': NPC clips play FROM the speaker; anchorless clips
            // (player voice, oracle, narration) reset to 2D - a stale blend must never leak.
            _currentAnchor = anchor;
            _source.spatialBlend = anchor != null ? 1f : 0f;
            if (anchor != null) transform.position = anchor.position;
            _source.pitch = pitch;
            _source.clip = clip;
            _source.Play();
        }

        internal static AudioClip TryLoadFinishedWavPublic(string path) => TryLoadFinishedWav(path);

        private static AudioClip TryLoadFinishedWav(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                byte[] bytes;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    bytes = new byte[fs.Length];
                    int read = 0;
                    while (read < bytes.Length) read += fs.Read(bytes, read, bytes.Length - read);
                }
                return ParsePcm16Wav(bytes, Path.GetFileNameWithoutExtension(path));
            }
            catch (IOException) { return null; } // exclusive open failed: writer still holds it
            catch (Exception e)
            {
                Debug.Log($"[Piper] bad wav {path}: {e.Message}");
                return AudioClip.Create("empty", 1, 1, 22050, false);
            }
        }

        private static AudioClip ParsePcm16Wav(byte[] wav, string name)
        {
            int channels = BitConverter.ToInt16(wav, 22);
            int sampleRate = BitConverter.ToInt32(wav, 24);
            // Find the 'data' chunk (piper writes canonical 44-byte headers, but scan anyway).
            int pos = 12;
            while (pos + 8 <= wav.Length)
            {
                bool isData = wav[pos] == 'd' && wav[pos + 1] == 'a' && wav[pos + 2] == 't' && wav[pos + 3] == 'a';
                int size = BitConverter.ToInt32(wav, pos + 4);
                if (isData)
                {
                    int samples = size / 2;
                    var data = new float[samples];
                    for (int i = 0; i < samples; i++)
                        data[i] = BitConverter.ToInt16(wav, pos + 8 + i * 2) / 32768f;
                    var clip = AudioClip.Create(name, samples / Mathf.Max(1, channels), channels, sampleRate, false);
                    clip.SetData(data, 0);
                    return clip;
                }
                pos += 8 + size;
            }
            throw new InvalidDataException("no data chunk");
        }
    }
}

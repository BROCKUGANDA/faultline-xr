namespace Faultline.Runtime.Audio
{
    using System;

    using UnityEngine;

    /// <summary>
    /// Synthesised interaction cues, mirroring the WebXR build so no audio
    /// assets or licences are required. Every cue has a visual equivalent in
    /// the panels, and the whole service can be muted from Settings without
    /// affecting any other behaviour.
    /// </summary>
    public sealed class AudioCueService : IDisposable
    {
        private readonly AudioSource _source;
        private readonly AudioMixerGroup _group;
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _clips = new();
        private bool _enabled = true;

        public AudioCueService(Transform parent, AudioMixerGroup group = null)
        {
            var go = new GameObject("FaultlineAudio");
            go.transform.SetParent(parent, false);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.22f;
            _group = group;
            if (_group != null) _source.outputAudioMixerGroup = _group;
        }

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public void Focus() => Play(nameof(Focus), 0.05f, t => Sin(t, 520f) * Envelope(t, 0.05f) * 0.35f);
        public void Select() => Play(nameof(Select), 0.04f, t => Sin(t, 760f) * Envelope(t, 0.04f) * 0.5f);
        public void Dispatch() => Play(nameof(Dispatch), 0.16f, t => Sin(t, Mathf.Lerp(330f, 620f, t / 0.16f)) * Envelope(t, 0.16f) * 0.45f);
        public void Alert() => Play(nameof(Alert), 0.22f, t => Sin(t, Mathf.Lerp(880f, 420f, t / 0.22f)) * Envelope(t, 0.22f) * 0.55f);
        public void Approval() => Play(nameof(Approval), 0.18f, t => (Sin(t, 523.25f) + Sin(t, 659.25f)) * 0.5f * Envelope(t, 0.18f) * 0.4f);

        public void Verified() => Play(nameof(Verified), 0.3f, t =>
            (Sin(t, 523.25f) + Sin(t, 784f) + Sin(t, 1046.5f)) * 0.33f * Envelope(t, 0.3f) * 0.45f);

        public void Dispose()
        {
            foreach (var clip in _clips.Values) UnityEngine.Object.Destroy(clip);
            _clips.Clear();
            if (_source != null) UnityEngine.Object.Destroy(_source.gameObject);
        }

        /// <summary>
        /// Cues are synthesised once and cached, so repeated interactions never
        /// allocate in the XR frame loop.
        /// </summary>
        private void Play(string cue, float duration, Func<float, float> generator)
        {
            if (!_enabled) return;
            if (!_clips.TryGetValue(cue, out var clip) || clip == null)
            {
                clip = BuildClip(duration, (t, d) => generator(t));
                _clips[cue] = clip;
            }
            _source.PlayOneShot(clip);
        }

        private static float Sin(float time, float frequency) =>
            Mathf.Sin(2f * Mathf.PI * frequency * time);

        /// <summary>Short attack, exponential-ish decay so cues never click.</summary>
        private static float Envelope(float time, float duration)
        {
            var normalised = Mathf.Clamp01(time / Mathf.Max(0.001f, duration));
            var attack = Mathf.Clamp01(normalised / 0.08f);
            var decay = Mathf.Pow(1f - normalised, 1.6f);
            return attack * decay;
        }

        private static AudioClip BuildClip(float duration, Func<float, float, float> generator)
        {
            var sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            var samples = Mathf.Max(16, Mathf.CeilToInt(duration * sampleRate));
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var time = (float)i / sampleRate;
                data[i] = generator(time, duration);
            }

            var clip = AudioClip.Create("FaultlineCue", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
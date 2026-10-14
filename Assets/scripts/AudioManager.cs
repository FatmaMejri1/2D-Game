using System.Collections.Generic;
using UnityEngine;

public enum Sfx
{
    Pickup, Coin, Gem, PowerUp, Door, Hurt, Win, Lose, Start,
    Locked, Lever, Teleport, Alert, Tick, Room, ShieldBreak, Jump
}

/// <summary>
/// Generates every sound and the background music in code, so no audio files are needed.
/// To use your own audio, put clips in Assets/Resources/ named:
///   Music, Music_Tense, and Sfx_Coin, Sfx_Gem, Sfx_Hurt, ... (Sfx_ + enum name). They override the generated ones.
/// Press M in game to mute.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    const int Rate = 44100;
    static readonly Dictionary<Sfx, AudioClip> generated = new Dictionary<Sfx, AudioClip>();
    static AudioClip calmMusic;
    static AudioClip tenseMusic;
    static bool muted;

    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    AudioSource sfxSource;
    AudioSource musicSource;
    bool tense;

    public bool Muted { get { return muted; } }

    void Awake()
    {
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
        musicSource.volume = 0.33f;

        BuildClips();
        AudioListener.volume = muted ? 0f : PlayerPrefs.GetFloat("EscapeRoomVolume", 1f);

        musicSource.clip = calmMusic;
        musicSource.Play();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildClips()
    {
        if (generated.Count == 0)
        {
            generated[Sfx.Pickup] = Tones("pickup", 0.35f, (660f, 0.08f), (880f, 0.08f), (1320f, 0.18f));
            generated[Sfx.Coin] = Tones("coin", 0.28f, (988f, 0.05f), (1319f, 0.14f));
            generated[Sfx.Gem] = Tones("gem", 0.3f, (1047f, 0.06f), (1319f, 0.06f), (1568f, 0.06f), (2093f, 0.22f));
            generated[Sfx.Start] = Tones("start", 0.3f, (440f, 0.08f), (660f, 0.12f));
            generated[Sfx.Locked] = Tones("locked", 0.3f, (180f, 0.1f), (150f, 0.15f));
            generated[Sfx.Win] = Tones("win", 0.35f, (523f, 0.12f), (659f, 0.12f), (784f, 0.12f), (1047f, 0.12f), (784f, 0.1f), (1047f, 0.45f));
            generated[Sfx.Lose] = Tones("lose", 0.4f, (392f, 0.2f), (311f, 0.2f), (262f, 0.55f));
            generated[Sfx.Tick] = Tones("tick", 0.25f, (1000f, 0.04f));
            generated[Sfx.Room] = Tones("room", 0.25f, (523f, 0.1f), (784f, 0.2f));
            generated[Sfx.Alert] = Tones("alert", 0.35f, (300f, 0.09f), (450f, 0.16f));
            generated[Sfx.Door] = Sweep("door", 200f, 500f, 0.45f, 0.35f, 0.05f);
            generated[Sfx.Hurt] = Sweep("hurt", 400f, 80f, 0.4f, 0.5f, 0.5f);
            generated[Sfx.PowerUp] = Sweep("powerup", 300f, 1000f, 0.35f, 0.3f, 0f);
            generated[Sfx.Lever] = Sweep("lever", 160f, 60f, 0.28f, 0.5f, 0.3f);
            generated[Sfx.Teleport] = Sweep("teleport", 300f, 1500f, 0.4f, 0.3f, 0.1f);
            generated[Sfx.Jump] = Sweep("jump", 280f, 640f, 0.16f, 0.25f, 0f);
            generated[Sfx.ShieldBreak] = Sweep("shieldbreak", 900f, 200f, 0.3f, 0.4f, 0.5f);
        }
        if (calmMusic == null) calmMusic = Music("music_calm", 96f, false);
        if (tenseMusic == null) tenseMusic = Music("music_tense", 150f, true);

        clips.Clear();
        foreach (var kv in generated) clips[kv.Key] = kv.Value;

        // optional user-provided overrides
        foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
        {
            var custom = Resources.Load<AudioClip>("Sfx_" + s);
            if (custom != null) clips[s] = custom;
        }
        var customMusic = Resources.Load<AudioClip>("Music");
        if (customMusic != null) calmMusic = customMusic;
        var customTense = Resources.Load<AudioClip>("Music_Tense");
        if (customTense != null) tenseMusic = customTense;
    }

    // ------------------------------------------------------------------ public API

    public static void Play(Sfx sfx)
    {
        if (Instance == null) return;
        AudioClip clip;
        if (Instance.clips.TryGetValue(sfx, out clip))
            Instance.sfxSource.PlayOneShot(clip, 1f);
    }

    public static void SetTense(bool value)
    {
        if (Instance == null || Instance.tense == value) return;
        Instance.tense = value;
        Instance.musicSource.clip = value ? tenseMusic : calmMusic;
        Instance.musicSource.Play();
    }

    public static void ToggleMute()
    {
        muted = !muted;
        AudioListener.volume = muted ? 0f : PlayerPrefs.GetFloat("EscapeRoomVolume", 1f);
        if (UIManager.Instance != null) UIManager.Instance.SetSound(!muted);
    }

    // ------------------------------------------------------------------ generators

    static AudioClip Tones(string name, float volume, params (float freq, float dur)[] notes)
    {
        float total = 0f;
        foreach (var n in notes) total += n.dur;
        int count = Mathf.CeilToInt(total * Rate);
        var data = new float[count];
        int idx = 0;
        foreach (var n in notes)
        {
            int len = Mathf.CeilToInt(n.dur * Rate);
            for (int i = 0; i < len && idx < count; i++, idx++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f - (float)i / len, Mathf.Clamp01(i / 200f));
                data[idx] = Mathf.Sin(2f * Mathf.PI * n.freq * t) * env * volume;
            }
        }
        return MakeClip(name, data);
    }

    static AudioClip Sweep(string name, float f0, float f1, float dur, float volume, float noise)
    {
        int count = Mathf.CeilToInt(dur * Rate);
        var data = new float[count];
        var rnd = new System.Random(7);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float p = (float)i / count;
            phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, p) / Rate;
            float s = Mathf.Sin(phase) * (1f - noise) + ((float)rnd.NextDouble() * 2f - 1f) * noise;
            data[i] = s * (1f - p) * volume;
        }
        return MakeClip(name, data);
    }

    // A looping 4-bar track in A minor (Am - F - C - G): bass, arpeggio, hi-hat and kick.
    static AudioClip Music(string name, float bpm, bool tense)
    {
        float step = 30f / bpm;                 // one eighth note
        const int steps = 32;
        int count = Mathf.CeilToInt(steps * step * Rate);
        var data = new float[count];

        float[] bass = { 110f, 87.31f, 130.81f, 98f };
        float[][] chords =
        {
            new[] { 220f, 261.63f, 329.63f },
            new[] { 174.61f, 220f, 261.63f },
            new[] { 261.63f, 329.63f, 392f },
            new[] { 196f, 246.94f, 293.66f }
        };
        int[] arp = { 0, 1, 2, 1, 0, 1, 2, 2 };
        var rnd = new System.Random(11);

        for (int s = 0; s < steps; s++)
        {
            int bar = s / 8;
            float t0 = s * step;

            if (tense || s % 2 == 0) AddNote(data, t0, step * 1.8f, bass[bar], 0.30f, 6f, 0.4f);

            float f = chords[bar][arp[s % 8]] * (tense ? 2f : 1f);
            AddNote(data, t0, step * 1.5f, f, 0.16f, 5f, 0.25f);

            if (tense || s % 2 == 1) AddNoise(data, t0, 0.05f, tense ? 0.10f : 0.06f, rnd);
            if (s % 4 == 0 || (tense && s % 4 == 2)) AddKick(data, t0, 0.35f);
        }

        float peak = 0.001f;
        for (int i = 0; i < count; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        float gain = 0.85f / peak;
        for (int i = 0; i < count; i++) data[i] *= gain;

        return MakeClip(name, data);
    }

    static void AddNote(float[] data, float start, float dur, float freq, float amp, float decay, float harm)
    {
        int i0 = Mathf.FloorToInt(start * Rate);
        int len = Mathf.CeilToInt(dur * Rate);
        for (int i = 0; i < len; i++)
        {
            int idx = i0 + i;
            if (idx >= data.Length) break;
            float t = (float)i / Rate;
            float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t / 0.004f);
            float w = 2f * Mathf.PI * freq * t;
            data[idx] += amp * env * (Mathf.Sin(w) + harm * Mathf.Sin(2f * w));
        }
    }

    static void AddNoise(float[] data, float start, float dur, float amp, System.Random rnd)
    {
        int i0 = Mathf.FloorToInt(start * Rate);
        int len = Mathf.CeilToInt(dur * Rate);
        for (int i = 0; i < len; i++)
        {
            int idx = i0 + i;
            if (idx >= data.Length) break;
            float t = (float)i / Rate;
            data[idx] += amp * Mathf.Exp(-t * 60f) * ((float)rnd.NextDouble() * 2f - 1f);
        }
    }

    static void AddKick(float[] data, float start, float amp)
    {
        int i0 = Mathf.FloorToInt(start * Rate);
        const float dur = 0.18f;
        int len = Mathf.CeilToInt(dur * Rate);
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            int idx = i0 + i;
            if (idx >= data.Length) break;
            float t = (float)i / Rate;
            phase += 2f * Mathf.PI * Mathf.Lerp(150f, 45f, t / dur) / Rate;
            data[idx] += amp * Mathf.Exp(-t * 18f) * Mathf.Sin(phase);
        }
    }

    static AudioClip MakeClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

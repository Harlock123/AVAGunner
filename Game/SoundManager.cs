using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NetCoreAudio;

namespace AVAGunner.Game;

public class SoundManager : IDisposable
{
    private string? _laserFile;
    private string? _explosionFile;
    private string? _enemyPassFile;
    private string? _gameOverFile;
    private string? _warpFile;
    private string? _shieldFile;
    private string? _turretFireFile;
    private string? _capitolExplosionFile;

    private readonly List<Player> _players = new();
    private const int MaxPlayers = 8;

    private bool _initialized;
    private bool _disposed;
    private string? _tempDir;

    public bool SoundEnabled { get; set; } = true;

    public void Initialize()
    {
        if (_initialized) return;

        try
        {
            // Create temp directory for sound files
            _tempDir = Path.Combine(Path.GetTempPath(), "AVAGunner_Sounds");
            Directory.CreateDirectory(_tempDir);

            // Generate sound files
            _laserFile = GenerateWavFile("laser", GenerateLaserWaveform());
            _explosionFile = GenerateWavFile("explosion", GenerateExplosionWaveform());
            _enemyPassFile = GenerateWavFile("enemypass", GenerateEnemyPassWaveform());
            _gameOverFile = GenerateWavFile("gameover", GenerateGameOverWaveform());
            _warpFile = GenerateWavFile("warp", GenerateWarpWaveform());
            _shieldFile = GenerateWavFile("shield", GenerateShieldWaveform());
            _turretFireFile = GenerateWavFile("turretfire", GenerateTurretFireWaveform());
            _capitolExplosionFile = GenerateWavFile("capitolexplosion", GenerateCapitolExplosionWaveform());

            // Pre-create players
            for (var i = 0; i < MaxPlayers; i++)
            {
                _players.Add(new Player());
            }

            _initialized = true;
        }
        catch
        {
            // Silently ignore audio initialization errors
        }
    }

    public void PlayLaser()
    {
        PlaySound(_laserFile);
    }

    public void PlayExplosion()
    {
        PlaySound(_explosionFile);
    }

    public void PlayEnemyPass()
    {
        PlaySound(_enemyPassFile);
    }

    public void PlayGameOver()
    {
        PlaySound(_gameOverFile);
    }

    public void PlayWarp()
    {
        PlaySound(_warpFile);
    }

    public void PlayShield()
    {
        PlaySound(_shieldFile);
    }

    public void PlayTurretFire()
    {
        PlaySound(_turretFireFile);
    }

    public void PlayCapitolExplosion()
    {
        PlaySound(_capitolExplosionFile);
    }

    private void PlaySound(string? filePath)
    {
        if (!_initialized || !SoundEnabled || string.IsNullOrEmpty(filePath)) return;

        try
        {
            // Find an available player
            foreach (var player in _players)
            {
                if (!player.Playing)
                {
                    _ = player.Play(filePath);
                    return;
                }
            }

            // All players busy, use first one anyway
            if (_players.Count > 0)
            {
                _ = _players[0].Play(filePath);
            }
        }
        catch
        {
            // Silently ignore playback errors
        }
    }

    private string GenerateWavFile(string name, short[] samples)
    {
        var filePath = Path.Combine(_tempDir!, $"{name}.wav");
        WriteWavFile(filePath, samples, 44100);
        return filePath;
    }

    private void WriteWavFile(string filePath, short[] samples, int sampleRate)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        using var writer = new BinaryWriter(stream);

        var numChannels = (short)1;
        var bitsPerSample = (short)16;
        var byteRate = sampleRate * numChannels * bitsPerSample / 8;
        var blockAlign = (short)(numChannels * bitsPerSample / 8);
        var dataSize = samples.Length * 2;

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        // fmt chunk
        writer.Write("fmt "u8);
        writer.Write(16); // Chunk size
        writer.Write((short)1); // Audio format (PCM)
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data chunk
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }

    private short[] GenerateLaserWaveform()
    {
        const int sampleRate = 44100;
        const float duration = 0.1f;
        var samples = new short[(int)(sampleRate * duration)];

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            // Frequency sweep from 1200Hz to 400Hz
            var freq = 1200f - progress * 800f;
            var amplitude = (1f - progress) * 0.8f;

            samples[i] = (short)(Math.Sin(2 * Math.PI * freq * t) * amplitude * 32767);
        }

        return samples;
    }

    private short[] GenerateExplosionWaveform()
    {
        const int sampleRate = 44100;
        const float duration = 0.5f;
        var samples = new short[(int)(sampleRate * duration)];
        var random = new Random(42);

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            // Initial punch/attack
            var attack = progress < 0.05 ? progress / 0.05 : 1.0;

            // Multi-layered explosion
            var noise = random.NextDouble() * 2 - 1;

            // Deep bass rumble
            var bassFreq = 40 + progress * 20;
            var bass = Math.Sin(2 * Math.PI * bassFreq * t) * 0.6;

            // Mid crunch
            var midFreq = 80 + progress * 40;
            var mid = Math.Sin(2 * Math.PI * midFreq * t) * 0.4;

            // High crackle (noise modulated)
            var crackle = noise * Math.Sin(2 * Math.PI * 200 * t) * 0.3;

            // Envelope with sharp attack, slow decay
            var envelope = attack * Math.Pow(1 - progress, 1.5);

            var sample = (bass + mid + crackle + noise * 0.4) * envelope;
            samples[i] = (short)(Math.Clamp(sample, -1, 1) * 32767);
        }

        // Heavier low-pass filter for more boom
        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 1; i < samples.Length; i++)
            {
                samples[i] = (short)(samples[i] * 0.25 + samples[i - 1] * 0.75);
            }
        }

        return samples;
    }

    private short[] GenerateEnemyPassWaveform()
    {
        const int sampleRate = 44100;
        const float duration = 0.25f;
        var samples = new short[(int)(sampleRate * duration)];

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            var freq = progress < 0.5f ? 200f : 150f;
            var envelope = Math.Sin(progress * Math.PI);

            samples[i] = (short)(Math.Sin(2 * Math.PI * freq * t) * envelope * 0.7 * 32767);
        }

        return samples;
    }

    private short[] GenerateGameOverWaveform()
    {
        const int sampleRate = 44100;
        const float duration = 1.0f;
        var samples = new short[(int)(sampleRate * duration)];
        var frequencies = new[] { 400f, 350f, 300f, 200f };
        var noteLength = duration / frequencies.Length;

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var noteIndex = (int)(t / noteLength);
            if (noteIndex >= frequencies.Length) noteIndex = frequencies.Length - 1;

            var noteT = (t % noteLength) / noteLength;
            var freq = frequencies[noteIndex];
            var envelope = 1f - noteT * 0.5f;

            var wave = Math.Sign(Math.Sin(2 * Math.PI * freq * t));
            samples[i] = (short)(wave * envelope * 0.5 * 32767);
        }

        return samples;
    }

    private short[] GenerateWarpWaveform()
    {
        const int sampleRate = 44100;
        const float duration = 1.2f;
        var samples = new short[(int)(sampleRate * duration)];

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            // Frequency rises then falls (warp in, warp out)
            float freq;
            float envelope;

            if (progress < 0.5f)
            {
                // Rising phase - accelerating into warp
                var riseProgress = progress * 2;
                freq = 100 + riseProgress * riseProgress * 800;
                envelope = riseProgress;
            }
            else
            {
                // Falling phase - exiting warp
                var fallProgress = (progress - 0.5f) * 2;
                freq = 900 - fallProgress * fallProgress * 700;
                envelope = 1 - fallProgress * 0.7f;
            }

            // Main warp tone
            var warp = Math.Sin(2 * Math.PI * freq * t);

            // Harmonic overtones for richness
            var harmonic1 = Math.Sin(2 * Math.PI * freq * 1.5 * t) * 0.3;
            var harmonic2 = Math.Sin(2 * Math.PI * freq * 2 * t) * 0.2;

            // Subtle whoosh noise
            var noise = (new Random(i).NextDouble() * 2 - 1) * 0.1 * (1 - Math.Abs(progress - 0.5) * 2);

            var sample = (warp + harmonic1 + harmonic2 + noise) * envelope * 0.6;
            samples[i] = (short)(Math.Clamp(sample, -1, 1) * 32767);
        }

        return samples;
    }

    private short[] GenerateShieldWaveform()
    {
        const int sampleRate = 44100;
        const float duration = 0.4f;
        var samples = new short[(int)(sampleRate * duration)];
        var random = new Random(77);

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            // Frequency sweep 200Hz → 800Hz (rising)
            var freq = 200f + progress * 600f;

            // Main tone
            var main = Math.Sin(2 * Math.PI * freq * t);

            // Harmonic overtone at 2x frequency for metallic quality
            var harmonic = Math.Sin(2 * Math.PI * freq * 2 * t) * 0.4;

            // White noise burst at start (0.05s) for "activation" feel
            var noise = 0.0;
            if (t < 0.05f)
            {
                noise = (random.NextDouble() * 2 - 1) * (1f - t / 0.05f) * 0.6;
            }

            // Slight resonance via feedback filter approximation
            var resonance = Math.Sin(2 * Math.PI * freq * 1.5 * t) * 0.15;

            // Amplitude envelope: sharp attack, sustain, medium decay
            float envelope;
            if (progress < 0.05f)
                envelope = progress / 0.05f; // Sharp attack
            else if (progress < 0.6f)
                envelope = 1f; // Sustain
            else
                envelope = 1f - (progress - 0.6f) / 0.4f; // Medium decay

            var sample = (main + harmonic + noise + resonance) * envelope * 0.5;
            samples[i] = (short)(Math.Clamp(sample, -1, 1) * 32767);
        }

        return samples;
    }

    private short[] GenerateTurretFireWaveform()
    {
        // Deep thump: 80Hz→40Hz sweep, 0.2s, low-pass filtered
        const int sampleRate = 44100;
        const float duration = 0.2f;
        var samples = new short[(int)(sampleRate * duration)];

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            // Frequency sweep from 80Hz to 40Hz
            var freq = 80f - progress * 40f;
            var envelope = (1f - progress) * (1f - progress);

            // Deep bass thump
            var main = Math.Sin(2 * Math.PI * freq * t);
            // Sub-harmonic for extra depth
            var sub = Math.Sin(2 * Math.PI * freq * 0.5 * t) * 0.5;

            var sample = (main + sub) * envelope * 0.7;
            samples[i] = (short)(Math.Clamp(sample, -1, 1) * 32767);
        }

        // Low-pass filter for deep sound
        for (var i = 1; i < samples.Length; i++)
        {
            samples[i] = (short)(samples[i] * 0.3 + samples[i - 1] * 0.7);
        }

        return samples;
    }

    private short[] GenerateCapitolExplosionWaveform()
    {
        // Three-phase: initial noise burst + deep bass rumble (30Hz) + crackle/debris
        const int sampleRate = 44100;
        const float duration = 1.5f;
        var samples = new short[(int)(sampleRate * duration)];
        var random = new Random(99);

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / sampleRate;
            var progress = t / duration;

            var noise = random.NextDouble() * 2 - 1;

            // Phase 1: Initial noise burst (0-0.1s)
            var burst = 0.0;
            if (t < 0.1f)
            {
                burst = noise * (1f - t / 0.1f) * 0.8;
            }

            // Phase 2: Deep bass rumble (30Hz)
            var bassFreq = 30 + progress * 15;
            var bass = Math.Sin(2 * Math.PI * bassFreq * t) * 0.8;
            var bassEnvelope = Math.Pow(1 - progress, 1.2);

            // Phase 3: Crackle/debris (noise modulated)
            var crackleFreq = 60 + progress * 30;
            var crackle = noise * Math.Sin(2 * Math.PI * crackleFreq * t) * 0.4;
            var crackleEnvelope = progress > 0.3f ? Math.Pow(1 - (progress - 0.3f) / 0.7f, 0.8) : progress / 0.3f;

            // Mid rumble layer
            var midFreq = 50 + progress * 20;
            var mid = Math.Sin(2 * Math.PI * midFreq * t) * 0.5;

            var sample = burst + (bass + mid) * bassEnvelope + crackle * crackleEnvelope + noise * 0.2 * bassEnvelope;
            samples[i] = (short)(Math.Clamp(sample, -1, 1) * 32767);
        }

        // Heavy 3-pass low-pass filter for deep, heavy sound
        for (var pass = 0; pass < 3; pass++)
        {
            for (var i = 1; i < samples.Length; i++)
            {
                samples[i] = (short)(samples[i] * 0.2 + samples[i - 1] * 0.8);
            }
        }

        return samples;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var player in _players)
        {
            try
            {
                player.Stop();
            }
            catch { }
        }

        // Clean up temp files
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch { }
        }
    }
}

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Corvax.CCCVars;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    [Dependency] private IConfigurationManager _cfg = default!;

    private HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly List<string> _cacheKeysSeq = new();
    private int _maxCachedCount = 200;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;
    private string _apiModel = string.Empty;
    private string _apiProxy = string.Empty;

    /// <summary>
    /// ElevenLabs output format: signed 16-bit little-endian PCM at 22050 Hz.
    /// Fastest format for real-time TTS.
    /// </summary>
    private const string OutputFormat = "pcm_22050";
    private const int PcmSampleRate = 22050;
    private const int PcmBitsPerSample = 16;
    private const int PcmChannels = 1;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiToken, v => _apiToken = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiModel, v => _apiModel = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiProxy, v =>
        {
            _apiProxy = v;
            UpdateHttpClient();
        }, true);
    }

    private void UpdateHttpClient()
    {
        _httpClient.Dispose();
        if (string.IsNullOrWhiteSpace(_apiProxy))
        {
            _httpClient = new HttpClient();
            return;
        }

        var handler = new HttpClientHandler
        {
            Proxy = new System.Net.WebProxy(_apiProxy),
            UseProxy = true
        };
        _httpClient = new HttpClient(handler);
    }

    /// <summary>
    /// Generates audio with passed text via ElevenLabs API.
    /// </summary>
    /// <param name="voiceId">ElevenLabs voice_id</param>
    /// <param name="text">Plain text to synthesize</param>
    /// <param name="isWhisper">If true, uses lower stability for whisper effect</param>
    /// <returns>WAV audio bytes or null if failed</returns>
    public async Task<byte[]?> ConvertTextToSpeech(string voiceId, string text, bool isWhisper = false)
    {
        WantedCount.Inc();
        var cacheKey = GenerateCacheKey(voiceId, text, isWhisper);
        if (_cache.TryGetValue(cacheKey, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Verbose($"Use cached sound for '{text}' speech by '{voiceId}' voice");
            return data;
        }

        _sawmill.Verbose($"Generate new audio for '{text}' speech by '{voiceId}' voice");

        var stability = isWhisper ? 0.2f : 0.5f;
        var similarityBoost = isWhisper ? 0.5f : 0.75f;

        var body = new ElevenLabsTtsRequest
        {
            Text = text,
            ModelId = _apiModel,
            VoiceSettings = new ElevenLabsVoiceSettings
            {
                Stability = stability,
                SimilarityBoost = similarityBoost,
            },
        };

        var url = $"{_apiUrl.TrimEnd('/')}/v1/text-to-speech/{voiceId}?output_format={OutputFormat}";

        var reqTime = DateTime.UtcNow;
        try
        {
            var timeout = _cfg.GetCVar(CCCVars.TTSApiTimeout);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("xi-api-key", _apiToken);
            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS request was rate limited by ElevenLabs");
                    return null;
                }

                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}, body: {errorBody}");
                return null;
            }

            // ElevenLabs returns raw audio bytes (PCM S16LE) directly in the response body
            var pcmData = await response.Content.ReadAsByteArrayAsync(cts.Token);
            if (pcmData.Length == 0)
            {
                _sawmill.Error($"TTS API returned empty audio data for '{text}'");
                return null;
            }

            NormalizePcmVolume(pcmData);

            // Wrap raw PCM in a WAV header so the audio engine can play it
            var wavData = WrapPcmInWav(pcmData);

            _cache.Add(cacheKey, wavData);
            _cacheKeysSeq.Add(cacheKey);
            if (_cache.Count > _maxCachedCount)
            {
                var firstKey = _cacheKeysSeq.First();
                _cache.Remove(firstKey);
                _cacheKeysSeq.Remove(firstKey);
            }

            _sawmill.Debug($"Generated new audio for '{text}' speech by '{voiceId}' voice ({wavData.Length} bytes)");
            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return wavData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{voiceId}' voice");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new sound for '{text}' speech by '{voiceId}' voice\n{e}");
            return null;
        }
    }

    public void ResetCache()
    {
        _cache.Clear();
        _cacheKeysSeq.Clear();
    }

    private string GenerateCacheKey(string voiceId, string text, bool isWhisper)
    {
        var key = $"{voiceId}/{text}/{isWhisper}";
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Wraps raw PCM S16LE data in a standard WAV (RIFF) header.
    /// </summary>
    private static byte[] WrapPcmInWav(byte[] pcmData)
    {
        const int headerSize = 44;
        var byteRate = PcmSampleRate * PcmChannels * PcmBitsPerSample / 8;
        var blockAlign = (short)(PcmChannels * PcmBitsPerSample / 8);
        var dataSize = pcmData.Length;
        var fileSize = headerSize + dataSize - 8; // RIFF chunk size = file size - 8

        using var ms = new MemoryStream(headerSize + dataSize);
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(fileSize);
        writer.Write("WAVE"u8);

        // fmt sub-chunk
        writer.Write("fmt "u8);
        writer.Write(16);                      // Sub-chunk size (16 for PCM)
        writer.Write((short)1);                // Audio format (1 = PCM)
        writer.Write((short)PcmChannels);      // Number of channels
        writer.Write(PcmSampleRate);           // Sample rate
        writer.Write(byteRate);                // Byte rate
        writer.Write(blockAlign);              // Block align
        writer.Write((short)PcmBitsPerSample); // Bits per sample

        // data sub-chunk
        writer.Write("data"u8);
        writer.Write(dataSize);
        writer.Write(pcmData);

        return ms.ToArray();
    }

    /// <summary>
    /// Нормализуем громкость PCM S16LE аудио, делая все голоса одинаково громкими.
    /// </summary>
    private static void NormalizePcmVolume(byte[] pcmData)
    {
        float maxAmplitude = 0;
        for (int i = 0; i < pcmData.Length; i += 2)
        {
            // Собираем 16-битный сэмпл (Little Endian)
            short sample = (short)(pcmData[i] | (pcmData[i + 1] << 8));
            float abs = Math.Abs(sample);
            if (abs > maxAmplitude) maxAmplitude = abs;
        }

        if (maxAmplitude < 1) return;

        // 32767 - это физический максимум для 16-битного звука.
        // Умножаем на 0.9 (90%), чтобы звук был громким, но не хрипел (без клиппинга).
        float multiplier = (32767f * 0.9f) / maxAmplitude;

        for (int i = 0; i < pcmData.Length; i += 2)
        {
            short sample = (short)(pcmData[i] | (pcmData[i + 1] << 8));
            short newSample = (short)(sample * multiplier);
            // Разбираем обратно на байты
            pcmData[i] = (byte)(newSample & 0xFF);
            pcmData[i + 1] = (byte)(newSample >> 8);
        }
    }

    // --- ElevenLabs API DTOs ---

    private struct ElevenLabsTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("model_id")]
        public string ModelId { get; set; }

        [JsonPropertyName("voice_settings")]
        public ElevenLabsVoiceSettings VoiceSettings { get; set; }
    }

    private struct ElevenLabsVoiceSettings
    {
        [JsonPropertyName("stability")]
        public float Stability { get; set; }

        [JsonPropertyName("similarity_boost")]
        public float SimilarityBoost { get; set; }
    }
}

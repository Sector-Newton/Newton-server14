// This file intentionally left minimal.
// SSML support was removed during migration from Silero AI to ElevenLabs.
// ElevenLabs does not support SSML; whisper effects are handled via voice_settings in TTSManager.

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    // Intentionally empty — SSML logic removed (ElevenLabs uses voice_settings instead).
}

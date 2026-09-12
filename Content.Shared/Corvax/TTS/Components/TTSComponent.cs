using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Corvax.TTS;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent, NetworkedComponent]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Прототип дефолтного TTS голоса по умолчанию. Используется, если у говорящего нет TTSComponent или в нём не указан голос.
    /// </summary>
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype>? VoicePrototypeId { get; set; } = "Empty"; // Newton
}

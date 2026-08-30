using Robust.Shared.Prototypes;

namespace Content.Server.Newton.DNA;

[RegisterComponent]
public sealed partial class DNAIdentificationComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionSaveDNA";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public string DNA = String.Empty;

    [DataField]
    public bool CanEmag = true;

    [DataField]
    public bool EmaggedLater = false;
}
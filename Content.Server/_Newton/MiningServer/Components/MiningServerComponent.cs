namespace Content.Server.Newton.MiningServer.Components;

[RegisterComponent]
public sealed partial class MiningServerComponent : Component
{
    [DataField("basepoints"), ViewVariables(VVAccess.ReadWrite)]
    public int BasePoints;

    [DataField("slot")]
    public string SlotID;
}
namespace Content.Server.Newton.MiningServer.Components;

[RegisterComponent]
public sealed partial class UpgraderDiskComponent : Component
{
    [DataField("pointsadd"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsAdd;

    [DataField("energyadd"), ViewVariables(VVAccess.ReadWrite)]
    public int EnergyAdd;
}
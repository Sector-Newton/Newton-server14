using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Newton.Administration.CustomAdminArena;

[Prototype]
public sealed partial class CustomAdminArenaPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Login = default!;

    [DataField]
    public string Path = default!;
}
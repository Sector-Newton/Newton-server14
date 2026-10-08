using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Shared.Research.Prototypes; // Newton

namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchConsoleComponent : Component
{
    /// <summary>
    /// The radio channel that the unlock announcements are broadcast to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Science";

    // Newton-start

    /// <summary>
    /// Selected discipline.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<TechDisciplinePrototype> SelectedDiscipline = "Industrial";

    // Newton-end
}


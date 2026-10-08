using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Content.Client.Research.UI;
using Content.Shared.Newton.Research;

namespace Content.Client.Newton.Research.UI;

[UsedImplicitly]
public sealed class NewResearchConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NewResearchConsoleMenu? _consoleMenu;

    public NewResearchConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var owner = Owner;

        _consoleMenu = this.CreateWindow<NewResearchConsoleMenu>();
        _consoleMenu.SetEntity(owner);

        _consoleMenu.OnTechnologyResearchPressed += id =>
        {
            SendMessage(new ConsoleUnlockTechnologyMessage(id));
        };

        _consoleMenu.OnServerButtonPressed += () =>
        {
            SendMessage(new ConsoleServerSelectionMessage());
        };
    }

    public override void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        base.OnProtoReload(args);

        if (!args.WasModified<TechnologyPrototype>())
            return;

        if (State is not ResearchConsoleBoundInterfaceState rState)
            return;

        _consoleMenu?.UpdateItems(rState.ResearchStatus);
        _consoleMenu?.UpdateInformationPanel(rState);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ResearchConsoleBoundInterfaceState castState)
            return;
        _consoleMenu?.UpdateItems(castState.ResearchStatus);
        _consoleMenu?.UpdateInformationPanel(castState);
    }
}

using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Newton.Research; // Newton
using Robust.Shared.Prototypes; // Newton
using System.Linq; // Newton

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!; // Newton

    private void InitializeConsole()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, ConsoleUnlockTechnologyMessage>(OnConsoleUnlock);
        SubscribeLocalEvent<ResearchConsoleComponent, BeforeActivatableUIOpenEvent>(OnConsoleBeforeUiOpened);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, ResearchRegistrationChangedEvent>(OnConsoleRegistrationChanged);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseModifiedEvent>(OnConsoleDatabaseModified);
        SubscribeLocalEvent<ResearchConsoleComponent, TechnologyDatabaseSynchronizedEvent>(OnConsoleDatabaseSynchronized);
        SubscribeLocalEvent<ResearchConsoleComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnConsoleUnlock(EntityUid uid, ResearchConsoleComponent component, ConsoleUnlockTechnologyMessage args)
    {
        var act = args.Actor;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!ProtoMan.TryIndex<TechnologyPrototype>(args.Id, out var technologyPrototype))
            return;

        if (TryComp<AccessReaderComponent>(uid, out var access) && !_accessReader.IsAllowed(act, uid, access))
        {
            _popup.PopupEntity(Loc.GetString("research-console-no-access-popup"), act);
            return;
        }

        if (!UnlockTechnology(uid, args.Id, act))
            return;

        if (!_emag.CheckFlag(uid, EmagType.Interaction))
        {

            var message = Loc.GetString(
                "research-console-unlock-technology-radio-broadcast",
                ("technology", Loc.GetString(technologyPrototype.Name)),
                ("amount", technologyPrototype.Cost),
                ("approver", _identity.GetIdentityShortInfo(act, uid) ?? string.Empty)
            );
            _radio.SendRadioMessage(uid, message, component.AnnouncementChannel, uid, escapeMarkup: false);
        }

        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleBeforeUiOpened(EntityUid uid, ResearchConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        SyncClientWithServer(uid);
    }
    // Newton-R&D-rework-start
    private void UpdateConsoleInterface(EntityUid uid, ResearchConsoleComponent? component = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref component, ref clientComponent, false))
            return;

        ResearchConsoleBoundInterfaceState state;
        Dictionary<string, ResearchStatus> researchStatusDict;
        TechnologyDatabaseComponent? syncedDb = null;
        HashSet<ProtoId<TechnologyPrototype>> available;
        HashSet<ProtoId<TechnologyPrototype>> unlocked;
        HashSet<ProtoId<TechnologyPrototype>> allTech;
        var points = 0;

        if (TryGetClientServer(uid, out var serverUid, out var serverComponent, clientComponent) && TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
        {
            points = clientComponent.ConnectedToServer ? serverComponent.Points : 0;
            syncedDb = db;
            available = new HashSet<ProtoId<TechnologyPrototype>>(db.AvailableTechnologies);
            unlocked = new HashSet<ProtoId<TechnologyPrototype>>(db.UnlockedTechnologies);
            allTech = new HashSet<ProtoId<TechnologyPrototype>>(db.AllTechnologies);

            researchStatusDict = allTech.ToDictionary(
                techId => techId.ToString(),
                techId =>
                {
                    var listUnlockedPrerequisites = new List<ProtoId<TechnologyPrototype>>();
                    var tech = _prototypeManager.Index(techId);
                    bool canResearch = tech.Cost <= serverComponent.Points;

                    tech.TechnologyPrerequisites.ForEach(i =>
                    {
                        if (unlocked.Contains(i))
                            listUnlockedPrerequisites.Add(i);
                    });

                    if (unlocked.Contains(techId))
                        return ResearchStatus.Researched;

                    if (!available.Contains(techId))
                    {
                        if (listUnlockedPrerequisites.Count > 0)
                            return ResearchStatus.Prerequisites;
                        else
                            return ResearchStatus.Unavailable;
                    }

                    return canResearch ? ResearchStatus.Available : ResearchStatus.Unavailable;
                }
            );

            if (clientComponent != null)
                points = clientComponent.ConnectedToServer ? serverComponent.Points : 0;
        }
        else
        {
            researchStatusDict = new Dictionary<string, ResearchStatus>();
        }

        state = new ResearchConsoleBoundInterfaceState(points, researchStatusDict);

        _uiSystem.SetUiState(uid, ResearchConsoleUiKey.Key, state);
    }
    // Newton-R&D-rework-end
    private void OnPointsChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchServerPointsChangedEvent args)
    {
        if (!_uiSystem.IsUiOpen(uid, ResearchConsoleUiKey.Key))
            return;
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleRegistrationChanged(EntityUid uid, ResearchConsoleComponent component, ref ResearchRegistrationChangedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseModified(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    private void OnConsoleDatabaseSynchronized(EntityUid uid, ResearchConsoleComponent component, ref TechnologyDatabaseSynchronizedEvent args)
    {
        UpdateConsoleInterface(uid, component);
    }

    private void OnEmagged(Entity<ResearchConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }
}

using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Newton.DNA;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Shared.Actions;
using Content.Shared.Newton.Actions;
using Content.Shared.Chat;
using Content.Shared.Forensics.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Gibbing;
using Content.Shared.Interaction.Components;
using Content.Shared.Speech;
using Content.Shared.Emag.Systems;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Newton.DNA;

public sealed class IdentificationSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private ExplosionSystem _explosionSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private GibbingSystem _gibbing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DNAIdentificationComponent, GotEquippedEvent>(OnEquip);
        // SubscribeLocalEvent<DNAIdentificationComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<DNAIdentificationComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<DNAIdentificationComponent, SaveDNAActionEvent>(OnSaveDNA);
        SubscribeLocalEvent<DNAIdentificationComponent, GotEmaggedEvent>(OnEmagged);
        // SubscribeLocalEvent<DnaComponent, GenerateDnaEvent>(OnDnaChanged); // Maybe later
    }

    private void OnGetActions(EntityUid uid, DNAIdentificationComponent comp, GetItemActionsEvent args)
    {
        if (comp.EmaggedLater) return;
    
        if (comp.DNA == String.Empty)
        {
            args.AddAction(ref comp.ActionEntity, comp.Action);
        }
    }

    public void OnSaveDNA(EntityUid uid, DNAIdentificationComponent comp, SaveDNAActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.DNA != String.Empty)
        {
            _popupSystem.PopupEntity(Loc.GetString("identification-dna-already-stored"), args.Performer, args.Performer);
        }
        else
        {
            if (TryComp(args.Performer, out DnaComponent? dna) && dna.DNA != null)
            {
                comp.DNA = dna.DNA;

                _popupSystem.PopupEntity(Loc.GetString("identification-dna-was-stored"), args.Performer, args.Performer);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("identification-dna-not-presented"), args.Performer, args.Performer);
            }
        }

        args.Handled = true;
    }

    public void OnEquip(EntityUid uid, DNAIdentificationComponent comp, GotEquippedEvent args)
    {
        if (comp.DNA == String.Empty || comp.EmaggedLater == true) return;

        if (TryComp(args.EquipTarget, out DnaComponent? dna) && comp.DNA == dna.DNA) return;

        _adminLogger.Add(LogType.Trigger, LogImpact.Medium,
            $"{ToPrettyString(args.EquipTarget):user} activated acidification system of {ToPrettyString(uid):target}");

        EnsureComp<UnremoveableComponent>(uid);
        EnsureComp<SpeechComponent>(uid);

        _popupSystem.PopupEntity(
            Loc.GetString("identification-error-spikes"),
            args.EquipTarget,
            args.EquipTarget,
            Shared.Popups.PopupType.LargeCaution);

        Timer.Spawn(1000,
            () => _chat.TrySendInGameICMessage(uid,
                Loc.GetString("identification-error"),
                InGameICChatType.Speak, true));

        Timer.Spawn(2000,
            () => _chat.TrySendInGameICMessage(uid, "3", InGameICChatType.Speak, true));

        Timer.Spawn(3000,
            () => _chat.TrySendInGameICMessage(uid, "2", InGameICChatType.Speak, true));

        Timer.Spawn(4000,
            () => _chat.TrySendInGameICMessage(uid, "1", InGameICChatType.Speak, true));

        Timer.Spawn(5000,
            () => TriggerExplode(uid, comp, args.EquipTarget));
    }

    private void TriggerExplode(EntityUid uid, DNAIdentificationComponent comp, EntityUid targetuid)
    {
        var coords = _transformSystem.GetMapCoordinates(uid);
        _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                        4, 1, 2, uid, maxTileBreak: 0);

        _gibbing.Gib(targetuid);
    }

    public void OnEmagged(EntityUid uid, DNAIdentificationComponent comp, GotEmaggedEvent args)
    {
        if (!comp.CanEmag)
            return;
    
        if (comp.EmaggedLater)
        {
            var coords = _transformSystem.GetMapCoordinates(uid);
            _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                            8, 2, 4, uid, maxTileBreak: 0);
        }
        else
        {
            comp.EmaggedLater = true;
            _popupSystem.PopupEntity(Loc.GetString("identification-on-emagged"), uid);
        }
    
        args.Handled = true;
    }
}
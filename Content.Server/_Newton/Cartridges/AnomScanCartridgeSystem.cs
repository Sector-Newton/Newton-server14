using Content.Shared.Anomaly.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared._Newton.Cartridges;

namespace Content.Server._Newton.Cartridges;

public sealed partial class AnomScanCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomScanCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<AnomScanCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<AnomScanCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        EnsureComp<AnomalyScannerComponent>(args.Loader);
    }

    private void OnCartridgeRemoved(Entity<AnomScanCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        // only remove when the program itself is removed
        if (!_cartridgeLoaderSystem.HasProgram<AnomScanCartridgeComponent>(args.Loader.AsNullable()))
        {
            RemComp<AnomalyScannerComponent>(args.Loader);
        }
    }
}

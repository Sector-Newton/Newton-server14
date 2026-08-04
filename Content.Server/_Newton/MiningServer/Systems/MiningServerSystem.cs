using Content.Server.Newton.MiningServer.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Newton.MiningServer.Systems;

public sealed partial class MiningServerSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MiningServerComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
    }

    private void OnGetPointsPerSecond(Entity<MiningServerComponent> source, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (CanProduce(source))
            args.Points += source.Comp.BasePoints;

        if (!TryComp<ItemSlotsComponent>(source.Owner, out var slot))
            return;
        
        if (!_itemSlots.TryGetSlot(source.Owner, source.Comp.SlotID, out var itemSlot, component: slot) || !itemSlot.HasItem)
            return;

        if (!TryComp<UpgraderDiskComponent>(itemSlot.Item, out var disk))
            return;
            
        if (CanProduce(source))
            args.Points += disk.PointsAdd;
    }

    public bool CanProduce(Entity<MiningServerComponent> source)
    {
        return this.IsPowered(source, EntityManager);
    }
}
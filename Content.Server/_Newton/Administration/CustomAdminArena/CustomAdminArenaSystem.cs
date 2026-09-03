using System.Diagnostics.CodeAnalysis;
using Content.Shared.Newton.Administration.CustomAdminArena;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server.Newton.Administration.CustomAdminArena;

public sealed partial class CustomAdminArenaSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _player = default!;

    public bool TryGetCustomAdminArena(
        EntityUid uid,
        [NotNullWhen(true)] out CustomAdminArenaPrototype? proto)
    {
        proto = null;

        if (!_player.TryGetSessionByEntity(uid, out var session))
            return false;

        var login = session.Name;
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CustomAdminArenaPrototype>())
        {
            if (!string.Equals(
                    prototype.Login,
                    login,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            proto = prototype;
            return true;
        }

        return false;
    }

    public bool TryGetCustomAdminArena(
        ICommonSession session,
        [NotNullWhen(true)] out CustomAdminArenaPrototype? proto)
    {
        proto = null;

        var login = session.Name;
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CustomAdminArenaPrototype>())
        {
            if (!string.Equals(
                    prototype.Login,
                    login,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            proto = prototype;
            return true;
        }

        return false;
    }
}
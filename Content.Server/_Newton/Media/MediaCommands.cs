using System.Data.Common;
using Content.Server.Administration.Managers;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Newton.Media;

[AdminCommand(AdminFlags.Permissions)]
public sealed partial class AddMediaCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private IServerDbManager _dbManager = default!;
    public override string Command => "addmedia";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isMedia = await _dbManager.GetMediaStatusAsync(guid);
            if (isMedia)
            {
                shell.WriteLine(Loc.GetString("cmd-addmedia-existing", ("username", data.Username)));
                return;
            }

            await _dbManager.AddToMediaAsync(guid);
            shell.WriteLine(Loc.GetString("cmd-addmedia-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-addmedia-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-addmedia-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Permissions)]
public sealed partial class RemoveMediaCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _locator = default!;
    [Dependency] private IServerDbManager _dbManager = default!;

    public override string Command => "removemedia";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isMedia = await _dbManager.GetMediaStatusAsync(guid);
            if (!isMedia)
            {
                shell.WriteLine(Loc.GetString("cmd-removemedia-existing", ("username", data.Username)));
                return;
            }

            await _dbManager.RemoveFromMediaAsync(guid);
            shell.WriteLine(Loc.GetString("cmd-removemedia-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-removemedia-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-removemedia-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed partial class MediaListCommand : LocalizedCommands
{
    [Dependency] private IServerDbManager _dbManager = default!;
    [Dependency] private IAdminManager _adminManager = default!;

    public override string Command => "medialist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {

        var Media = await _dbManager.GetAllMediaStatusAsync();
        bool SeeAll = false;

        if (shell.Player != null)
            SeeAll = _adminManager.HasAdminFlag(shell.Player, AdminFlags.Permissions) || _adminManager.HasAdminFlag(shell.Player, AdminFlags.Pii);

        if (Media != null)
        {
            foreach (var i in Media)
            {
                var userId = new NetUserId(i);
                var data = await _dbManager.GetPlayerRecordByUserId(userId);

                if (data != null)
                    if (SeeAll)
                        shell.WriteLine(Loc.GetString("cmd-medialist-line-seeall", ("username", data.LastSeenUserName), ("guid", i.ToString())));
                    else
                        shell.WriteLine(Loc.GetString("cmd-medialist-line", ("username", data.LastSeenUserName)));
            }
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-medialist-notfound"));
        }
    }
}
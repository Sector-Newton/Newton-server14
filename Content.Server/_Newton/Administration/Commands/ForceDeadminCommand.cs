using Content.Server.Administration.Managers;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Newton.Administration.Commands;

[AdminCommand(AdminFlags.Permissions)]
public sealed partial class PlayTimeAddOverallCommand : IConsoleCommand
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IAdminManager _adminManager = default!;

    public string Command => "forcedeadmin";
    public string Description => Loc.GetString("cmd-forcedeadmin-desc");
    public string Help => Loc.GetString("cmd-forcedeadmin-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-forcedeadmin-error-args"));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
            return;
        }

        if (_adminManager.HasAdminFlag(player, AdminFlags.Bypass))
        {
            shell.WriteError(Loc.GetString("cmd-forcedeadmin-has-flags"));
            return;
        }

        var data = _adminManager.GetAdminData(player);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-forcedeadmin-non-admin", ("username", args[0])));
            return;
        }

        if (!data.Active)
        {
            shell.WriteError(Loc.GetString("cmd-forcedeadmin-in-deadmin", ("username", args[0])));
            return;
        }

        _adminManager.DeAdmin(player);

        shell.WriteLine(Loc.GetString(
            "cmd-forcedeadmin-succeed",
            ("username", args[0])));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-forcedeadmin-arg-user"));

        return CompletionResult.Empty;
    }
}
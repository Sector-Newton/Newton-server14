using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Newton.CCVars;

[CVarDefs]
public sealed partial class NewtonCCVars : CVars
{
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordNotesWebhook =
        CVarDef.Create("discord.notes_webhook", string.Empty, CVar.SERVERONLY);
}
using System.Text;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Notes;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared._Newton.CCVars;
using CCVars = Content.Shared.CCVar.CCVars;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.Administration.Notes;

public sealed partial class AdminNotesManager : IAdminNotesManager, IPostInjectInit
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private EuiManager _euis = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IConfigurationManager _config = default!;

    public const string SawmillId = "admin.notes";

    // Newton-noteswebhook-start
    private string _webhookName = "Newton";
    private string _webhookAvatarUrl = "https://media.istockphoto.com/id/1393248253/ru/%D0%B2%D0%B5%D0%BA%D1%82%D0%BE%D1%80%D0%BD%D0%B0%D1%8F/%D0%B1%D1%83%D0%BA%D0%B2%D0%B0-n-%D0%BA%D0%BE%D1%80%D0%BE%D0%BD%D0%B0-%D0%BB%D0%BE%D0%B3%D0%BE%D1%82%D0%B8%D0%BF-%D0%BB%D0%BE%D0%B3%D0%BE%D1%82%D0%B8%D0%BF-%D0%BA%D0%BE%D1%80%D0%BE%D0%BD%D1%8B-%D0%BD%D0%B0-%D0%B1%D1%83%D0%BA%D0%B2%D0%B5-n-%D0%B2%D0%B5%D0%BA%D1%82%D0%BE%D1%80%D0%BD%D1%8B%D0%B9-%D1%88%D0%B0%D0%B1%D0%BB%D0%BE%D0%BD-%D0%B4%D0%BB%D1%8F-%D0%BA%D1%80%D0%B0%D1%81%D0%BE%D1%82%D1%8B-%D0%BC%D0%BE%D0%B4%D1%8B-%D0%B7%D0%B2%D0%B5%D0%B7%D0%B4%D1%8B.jpg?s=170667a&w=0&k=20&c=vY0HzM7NdIdv0cfO6chFTTGDEty9rQmAjIl09mT2rpo=";
    private string _webhook = string.Empty;
    // Newton-noteswebhook-end

    public event Action<SharedAdminNote>? NoteAdded;
    public event Action<SharedAdminNote>? NoteModified;
    public event Action<SharedAdminNote>? NoteDeleted;

    private ISawmill _sawmill = default!;

    public bool CanCreate(ICommonSession admin)
    {
        return CanEdit(admin);
    }

    public bool CanDelete(ICommonSession admin)
    {
        return CanEdit(admin);
    }

    public bool CanEdit(ICommonSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.EditNotes);
    }

    public bool CanView(ICommonSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.ViewNotes);
    }

    public async Task OpenEui(ICommonSession admin, NetUserId notedPlayer)
    {
        var ui = new AdminNotesEui();
        _euis.OpenEui(ui, admin);

        await ui.ChangeNotedPlayer(notedPlayer);
    }

    public async Task OpenUserNotesEui(ICommonSession player)
    {
        var ui = new UserNotesEui();
        _euis.OpenEui(ui, player);

        await ui.UpdateNotes();
    }

    public async Task AddAdminRemark(ICommonSession createdBy, Guid player, NoteType type, string message, NoteSeverity? severity, bool secret, DateTime? expiryTime)
    {
        message = message.Trim();

        // There's a foreign key constraint in place here. If there's no player record, it will fail.
        // Not like there's much use in adding notes on accounts that have never connected.
        // You can still ban them just fine, which is why we should allow admins to view their bans with the notes panel
        var playerRecord = await _db.GetPlayerRecordByUserId((NetUserId) player);
        if (playerRecord is null)
            return;

        var sb = new StringBuilder($"{createdBy.Name} added a");

        if (secret && type == NoteType.Note)
        {
            sb.Append(" secret");
        }

        sb.Append($" {type} with message {message}");

        switch (type)
        {
            case NoteType.Note:
                sb.Append($" with {severity} severity");
                break;
            case NoteType.Message:
                severity = null;
                secret = false;
                break;
            case NoteType.Watchlist:
                severity = null;
                secret = true;
                break;
            case NoteType.ServerBan:
            case NoteType.RoleBan:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        if (expiryTime is not null)
        {
            sb.Append($" which expires on {expiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");
        }

        _sawmill.Info(sb.ToString());

        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        var serverName = _config.GetCVar(CCVars.AdminLogsServerName); // This could probably be done another way, but this is fine. For displaying only.
        var createdAt = DateTime.UtcNow;
        var playtime = (await _db.GetPlayTimes(player)).Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)?.TimeSpent ?? TimeSpan.Zero;
        int noteId;
        bool? seen = null;

        switch (type)
        {
            case NoteType.Note:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a note", nameof(severity));
                noteId = await _db.AddAdminNote(roundId, player, playtime, message, severity.Value, secret, createdBy.UserId, createdAt, expiryTime);
                break;
            case NoteType.Watchlist:
                secret = true;
                noteId = await _db.AddAdminWatchlist(roundId, player, playtime, message, createdBy.UserId, createdAt, expiryTime);
                break;
            case NoteType.Message:
                noteId = await _db.AddAdminMessage(roundId, player, playtime, message, createdBy.UserId, createdAt, expiryTime);
                seen = false;
                break;
            case NoteType.ServerBan: // Add bans using the ban panel, not note edit
            case NoteType.RoleBan:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        var note = new SharedAdminNote(
            noteId,
            [(NetUserId) player],
            roundId.HasValue ? [roundId.Value] : [],
            serverName,
            playtime,
            type,
            message,
            severity,
            secret,
            createdBy.Name,
            createdBy.Name,
            createdAt,
            createdAt,
            expiryTime,
            null,
            null,
            null,
            seen
        );
        NoteAdded?.Invoke(note);
        // Newton-noteswebhook-start
        DateTimeOffset? expires = null;
        NoteSeverity severityWebhook = NoteSeverity.None;

        if (expiryTime != null)
            expires = new DateTimeOffset(expiryTime.Value);

        string expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        if (severity != null)
            severityWebhook = severity.Value;
        
        if (!secret)
            SendWebhook(await GenerateNotePayload(createdBy.Name, playerRecord.LastSeenUserName, message, severityWebhook, expiresString));
        // Newton-noteswebhook-end
    }

    private async Task<SharedAdminNote?> GetAdminRemark(int id, NoteType type)
    {
        return type switch
        {
            NoteType.Note => (await _db.GetAdminNote(id))?.ToShared(),
            NoteType.Watchlist => (await _db.GetAdminWatchlist(id))?.ToShared(),
            NoteType.Message => (await _db.GetAdminMessage(id))?.ToShared(),
            NoteType.ServerBan or NoteType.RoleBan => (await _db.GetBanAsNoteAsync(id))?.ToShared(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type")
        };
    }

    public async Task DeleteAdminRemark(int noteId, NoteType type, ICommonSession deletedBy)
    {
        var note = await GetAdminRemark(noteId, type);
        if (note == null)
        {
            _sawmill.Warning($"Player {deletedBy.Name} has tried to delete non-existent {type} {noteId}");
            return;
        }

        var deletedAt = DateTime.UtcNow;

        switch (type)
        {
            case NoteType.Note:
                await _db.DeleteAdminNote(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.Watchlist:
                await _db.DeleteAdminWatchlist(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.Message:
                await _db.DeleteAdminMessage(noteId, deletedBy.UserId, deletedAt);
                break;
            case NoteType.ServerBan or NoteType.RoleBan:
                await _db.HideBanFromNotes(noteId, deletedBy.UserId, deletedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        _sawmill.Info($"{deletedBy.Name} has deleted {type} {noteId}");
        NoteDeleted?.Invoke(note);
    }

    public async Task ModifyAdminRemark(int noteId, NoteType type, ICommonSession editedBy, string message, NoteSeverity? severity, bool secret, DateTime? expiryTime)
    {
        message = message.Trim();

        var note = await GetAdminRemark(noteId, type);

        // If the note doesn't exist or is the same, we skip updating it
        if (note == null ||
            note.Message == message &&
            note.NoteSeverity == severity &&
            note.Secret == secret &&
            note.ExpiryTime == expiryTime)
        {
            return;
        }

        var sb = new StringBuilder($"{editedBy.Name} has modified {type} {noteId}");

        if (note.Message != message)
        {
            sb.Append($", modified message from {note.Message} to {message}");
        }

        if (note.Secret != secret)
        {
            sb.Append($", made it {(secret ? "secret" : "visible")}");
        }

        if (note.NoteSeverity != severity)
        {
            sb.Append($", updated the severity from {note.NoteSeverity} to {severity}");
        }

        if (note.ExpiryTime != expiryTime)
        {
            sb.Append(", updated the expiry time from ");
            if (note.ExpiryTime is null)
                sb.Append("never");
            else
                sb.Append($"{note.ExpiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");

            sb.Append(" to ");

            if (expiryTime is null)
                sb.Append("never");
            else
                sb.Append($"{expiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");
        }

        _sawmill.Info(sb.ToString());

        var editedAt = DateTime.UtcNow;

        switch (type)
        {
            case NoteType.Note:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a note", nameof(severity));
                await _db.EditAdminNote(noteId, message, severity.Value, secret, editedBy.UserId, editedAt, expiryTime);
                break;
            case NoteType.Watchlist:
                await _db.EditAdminWatchlist(noteId, message, editedBy.UserId, editedAt, expiryTime);
                break;
            case NoteType.Message:
                await _db.EditAdminMessage(noteId, message, editedBy.UserId, editedAt, expiryTime);
                break;
            case NoteType.ServerBan or NoteType.RoleBan:
                if (severity is null)
                    throw new ArgumentException("Severity cannot be null for a ban", nameof(severity));
                await _db.EditBan(noteId, message, severity.Value, expiryTime, editedBy.UserId, editedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        var newNote = note with
        {
            Message = message,
            NoteSeverity = severity,
            Secret = secret,
            LastEditedAt = editedAt,
            EditedByName = editedBy.Name,
            ExpiryTime = expiryTime
        };
        NoteModified?.Invoke(newNote);
    }

    public async Task<List<IAdminRemarksRecord>> GetAllAdminRemarks(Guid player)
    {
        return await _db.GetAllAdminRemarks(player);
    }

    public async Task<List<IAdminRemarksRecord>> GetVisibleRemarks(Guid player)
    {
        if (_config.GetCVar(CCVars.SeeOwnNotes))
        {
            return await _db.GetVisibleAdminNotes(player);
        }
        _sawmill.Warning($"Someone tried to call GetVisibleNotes for {player} when see_own_notes was false");
        return new List<IAdminRemarksRecord>();
    }

    public async Task<List<AdminWatchlistRecord>> GetActiveWatchlists(Guid player)
    {
        return await _db.GetActiveWatchlists(player);
    }

    public async Task<List<AdminMessageRecord>> GetNewMessages(Guid player)
    {
        return await _db.GetMessages(player);
    }

    public async Task MarkMessageAsSeen(int id, bool dismissedToo)
    {
        await _db.MarkMessageAsSeen(id, dismissedToo);
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
        _config.OnValueChanged(NewtonCCVars.DiscordNotesWebhook, OnWebhookChanged, true); // Newton-noteswebhook
    }
    // Newton-noteswebhook-start
    #region "webhook"

    private async void SendWebhook(WebhookPayload payload)
    {
        if (_webhook == string.Empty) return;

        var client = new HttpClient();

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var request = await client.PostAsync(_webhook, content);
        var requestContent = await request.Content.ReadAsStringAsync();

        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {requestContent}");
            return;
        }
    }

    private async Task<WebhookPayload> GenerateNotePayload(string adminName, string targetName, string reason, NoteSeverity noteSeverity, string expires)
    {
        var severity = "";

        switch (noteSeverity)
        {
            case NoteSeverity.None:
                severity = "Нету";
                break;
            case NoteSeverity.Minor:
                severity = "Низкая";
                break;
            case NoteSeverity.Medium:
                severity = "Средняя";
                break;
            case NoteSeverity.High:
                severity = "Высокая";
                break;
        }

        var description = "**Администратор:** \n> " + adminName + "\n**Игрок:** \n> " + targetName + "\n**Причина:** \n> " + reason + "\n**Степень тяжести:** \n> " + severity + "\n**Истечёт:** \n> " + expires;

        return new WebhookPayload
        {
            Username = _webhookName,
            AvatarUrl = _webhookAvatarUrl,
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = "Предупреждение",
                    Description = description,
                    Color = 16776960
                }
            }
        };
    }

    private void OnWebhookChanged(string url)
    {
        _webhook = url;

        _sawmill.Info($"notes webhook changed {_webhook}");

        if (url == string.Empty)
            return;
    }

    private struct WebhookPayload
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; } = "";

        [JsonPropertyName("embeds")]
        public List<Embed>? Embeds { get; set; } = null;

        public WebhookPayload()
        {
            
        }
    }

    private struct Embed
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("color")]
        public int Color { get; set; } = 0;

        public Embed()
        {
        }
    }

    #endregion
    // Newton-noteswebhook-end
}

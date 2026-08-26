using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Content.Server.Newton.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Newton.Administration.Managers;

public interface IWebhookManager
{
    public void Initialize();

    public Task<bool> SendWebhook(WebhookPayload payload, string url);

    public Task<bool> SendBanWebhook(WebhookPayload payload);
    public Task<bool> SendNotesWebhook(WebhookPayload payload);

    
}

public struct WebhookPayload
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

    public struct Embed
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("color")]
        public int Color { get; set; } = 0;

        [JsonPropertyName("footer")]
        public EmbedFooter? Footer { get; set; } = null;

        public Embed()
        {
        }
    }

    public struct EmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedFooter()
        {
        }
    }
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Content.Server.Newton.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared._Newton.CCVars;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using CCVars = Content.Shared.CCVar.CCVars;
using System.Net.Http.Headers;

namespace Content.Server.Newton.Administration.Managers;

public sealed partial class WebhookManager : IWebhookManager, IPostInjectInit
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;

    private string _banWeebhook = string.Empty;
    private string _notesWeebhook = string.Empty;
    private ISawmill _sawmill = default!;
    public const string SawmillId = "server.webhooks";

    public void Initialize()
    {
        _cfg.OnValueChanged(NewtonCCVars.DiscordBanWebhook, OnBanWebhookChanged, true);
        _cfg.OnValueChanged(NewtonCCVars.DiscordNotesWebhook, OnNotesWebhookChanged, true);
    }

    public async Task<bool> SendWebhook(WebhookPayload payload, string url)
    {
        if (url == string.Empty) return false;

        var client = new HttpClient();

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var request = await client.PostAsync(url, content);
        var requestContent = await request.Content.ReadAsStringAsync();

        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {requestContent}");
            return false;
        }

        return true;
    }

    public async Task<bool> SendBanWebhook(WebhookPayload payload)
    {
        if (_banWeebhook == string.Empty) return false;

        var client = new HttpClient();

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var request = await client.PostAsync(_banWeebhook, content);
        var requestContent = await request.Content.ReadAsStringAsync();

        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {requestContent}");
            return false;
        }

        return true;
    }

    public async Task<bool> SendNotesWebhook(WebhookPayload payload)
    {
        if (_notesWeebhook == string.Empty) return false;

        var client = new HttpClient();

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var request = await client.PostAsync(_notesWeebhook, content);
        var requestContent = await request.Content.ReadAsStringAsync();

        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {requestContent}");
            return false;
        }

        return true;
    }

    private void OnBanWebhookChanged(string url)
    {
        _banWeebhook = url;
    }

    private void OnNotesWebhookChanged(string url)
    {
        _notesWeebhook = url;
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }
}
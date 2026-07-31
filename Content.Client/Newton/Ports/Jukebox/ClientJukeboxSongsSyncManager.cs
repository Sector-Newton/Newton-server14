using Content.Shared.Newton.Ports.Jukebox;

namespace Content.Client.Newton.Ports.Jukebox;

public sealed class ClientJukeboxSongsSyncManager : JukeboxSongsSyncManager
{
    public override void OnSongUploaded(JukeboxSongUploadNetMessage message)
    {
        ContentRoot.AddOrUpdateFile(message.RelativePath!, message.Data);
    }
}

namespace DvrWorker.Models;

public sealed class Device
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Site { get; set; } = "HQ";
    public string Brand { get; set; } = "Hikvision";
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 80;


    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!; // TODO: Encrypt 


    public List<Channel> Channels = new();
    public bool IsEnabled { get; set; } = true;
}

public sealed class Channel
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
public sealed class Snapshots
{

}
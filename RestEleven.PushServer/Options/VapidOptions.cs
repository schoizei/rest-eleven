namespace RestEleven.PushServer.Options;

public class VapidOptions
{
    public string Subject { get; set; } = "mailto:admin@resteleven.local";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

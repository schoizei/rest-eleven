namespace RestEleven.Client.Options;

public class ClientOptions
{
    public Uri PushServerBaseUrl { get; set; } = new("https://localhost:7100");
    public Uri PersonioBridgeBaseUrl { get; set; } = new("https://localhost:7200");
    public string VapidPublicKey { get; set; } = string.Empty;
    public ReminderDefaults ReminderDefaults { get; set; } = new();
}

public class ReminderDefaults
{
    public bool Enabled { get; set; } = true;
    public string PreferredTime { get; set; } = "08:30";
    public int LeadMinutes { get; set; } = 15;
}

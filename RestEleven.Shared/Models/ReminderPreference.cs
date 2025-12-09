namespace RestEleven.Shared.Models;

public class ReminderPreference
{
    public bool Enabled { get; set; }
    public TimeOnly PreferredTime { get; set; } = new(9, 0);
    public int LeadMinutes { get; set; } = 15;
}

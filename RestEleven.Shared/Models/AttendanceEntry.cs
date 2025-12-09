namespace RestEleven.Shared.Models;

public class AttendanceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public int BreakMinutes { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedUtc { get; set; }

    public int WorkedMinutes => (int)(End.ToTimeSpan() - Start.ToTimeSpan()).TotalMinutes - BreakMinutes;
}

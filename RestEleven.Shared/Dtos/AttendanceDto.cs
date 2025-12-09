namespace RestEleven.Shared.Dtos;

public record AttendanceDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly Start { get; init; }
    public TimeOnly End { get; init; }
    public int BreakMinutes { get; init; }
    public string? Comment { get; init; }
    public double Confidence { get; init; }
    public DateTime SubmittedUtc { get; init; }
}

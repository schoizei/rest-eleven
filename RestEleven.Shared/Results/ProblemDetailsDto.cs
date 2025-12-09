namespace RestEleven.Shared.Results;

public record ProblemDetailsDto
{
    public string Title { get; init; } = string.Empty;
    public int? Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

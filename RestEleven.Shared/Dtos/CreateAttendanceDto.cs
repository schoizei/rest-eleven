using System.ComponentModel.DataAnnotations;

namespace RestEleven.Shared.Dtos;

public class CreateAttendanceDto
{
    [Required]
    public DateOnly Date { get; init; }

    [Required]
    public TimeOnly Start { get; init; }

    [Required]
    public TimeOnly End { get; init; }

    [Range(0, 180)]
    public int BreakMinutes { get; init; }

    [StringLength(256)]
    public string? Comment { get; init; }

    public bool AutoSubmitToPersonio { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace RestEleven.Shared.Dtos;

public class NotificationDto
{
    [Required]
    public string Title { get; init; } = string.Empty;

    public string? Body { get; init; }

    [Url]
    public string? Url { get; init; }
}

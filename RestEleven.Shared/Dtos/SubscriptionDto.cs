using System.ComponentModel.DataAnnotations;

namespace RestEleven.Shared.Dtos;

public class SubscriptionDto
{
    [Required]
    [Url]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string P256dh { get; init; } = string.Empty;

    [Required]
    public string Auth { get; init; } = string.Empty;

    public string? ClientId { get; init; }
}

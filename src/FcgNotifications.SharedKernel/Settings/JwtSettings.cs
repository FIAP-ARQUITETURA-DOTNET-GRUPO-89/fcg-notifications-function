using System.ComponentModel.DataAnnotations;

namespace FcgNotifications.SharedKernel.Settings;

public record JwtSettings
{
    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string SecurityKey { get; init; } = string.Empty;

    [Range(1, 24)]
    public int ExpirationHours { get; init; }
}


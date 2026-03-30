using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// An organisation (tenant) in the FoundryDB platform.
/// </summary>
public class Organization
{
    /// <summary>Unique organisation identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the organisation.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    /// <summary>Whether this is the user's personal organisation.</summary>
    [JsonPropertyName("is_personal")]
    public bool? IsPersonal { get; set; }

    /// <summary>Role of the authenticated user within this organisation.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>ISO-8601 timestamp when the organisation was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}

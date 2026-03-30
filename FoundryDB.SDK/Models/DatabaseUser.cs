using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// A database-level user managed by FoundryDB.
/// </summary>
public class DatabaseUser
{
    /// <summary>Username within the database engine.</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Whether this is the primary admin user created during provisioning.</summary>
    [JsonPropertyName("is_admin")]
    public bool? IsAdmin { get; set; }

    /// <summary>Database or databases this user has access to.</summary>
    [JsonPropertyName("databases")]
    public List<string>? Databases { get; set; }

    /// <summary>ISO-8601 timestamp when the user was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}

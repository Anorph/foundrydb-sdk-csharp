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

    /// <summary>Roles granted to this user within the database engine.</summary>
    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }

    /// <summary>ISO-8601 timestamp when the user was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}

/// <summary>
/// Full connection credentials returned by the reveal-password endpoint.
/// </summary>
public class RevealPasswordResponse
{
    /// <summary>Database username.</summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Plaintext password.</summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Hostname or IP to connect to.</summary>
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    /// <summary>TCP port for the database connection.</summary>
    [JsonPropertyName("port")]
    public long? Port { get; set; }

    /// <summary>Default database name.</summary>
    [JsonPropertyName("database")]
    public string? Database { get; set; }

    /// <summary>Ready-to-use connection string.</summary>
    [JsonPropertyName("connection_string")]
    public string? ConnectionString { get; set; }
}

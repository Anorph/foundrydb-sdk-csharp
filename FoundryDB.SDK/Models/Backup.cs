using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// A backup record for a managed service.
/// </summary>
public class Backup
{
    /// <summary>Unique backup identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Service this backup belongs to.</summary>
    [JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Current backup status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Backup type (full or incremental).</summary>
    [JsonPropertyName("backup_type")]
    public string? BackupType { get; set; }

    /// <summary>Size of the backup in bytes.</summary>
    [JsonPropertyName("size_bytes")]
    public long? SizeBytes { get; set; }

    /// <summary>Human-readable size label (e.g. "1.2 GB").</summary>
    [JsonPropertyName("size_label")]
    public string? SizeLabel { get; set; }

    /// <summary>Error message if the backup failed.</summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>ISO-8601 timestamp when the backup was requested.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp when the backup completed or failed.</summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}

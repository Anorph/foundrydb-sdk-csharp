using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// Request body for creating a new managed database service.
/// </summary>
public class CreateServiceRequest
{
    /// <summary>Display name for the service. Required.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Database engine type. Required.</summary>
    [JsonPropertyName("database_type")]
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// Major version string (e.g. "17" for PostgreSQL 17, "8.4" for MySQL 8.4).
    /// When omitted the platform picks the latest stable version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>Compute plan name (e.g. "tier-2"). Required.</summary>
    [JsonPropertyName("plan_name")]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>UpCloud zone slug (e.g. "se-sto1"). Defaults to "se-sto1" when omitted.</summary>
    [JsonPropertyName("zone")]
    public string? Zone { get; set; }

    /// <summary>Data disk size in gigabytes.</summary>
    [JsonPropertyName("storage_size_gb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StorageSizeGb { get; set; }

    /// <summary>Storage tier: "standard" or "maxiops" (NVMe).</summary>
    [JsonPropertyName("storage_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StorageTier { get; set; }

    /// <summary>Number of nodes for multi-node clusters.</summary>
    [JsonPropertyName("node_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NodeCount { get; set; }

    /// <summary>Enable automated failover for multi-node clusters.</summary>
    [JsonPropertyName("auto_failover_enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AutoFailoverEnabled { get; set; }

    /// <summary>Replication mode (e.g. "sync", "async").</summary>
    [JsonPropertyName("replication_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplicationMode { get; set; }

    /// <summary>Enable encryption at rest.</summary>
    [JsonPropertyName("encryption_enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EncryptionEnabled { get; set; }

    /// <summary>CIDR blocks permitted to connect (e.g. ["203.0.113.0/24"]).</summary>
    [JsonPropertyName("allowed_cidrs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? AllowedCidrs { get; set; }

    /// <summary>
    /// Per-request organisation override. When set, the SDK sends this value in
    /// X-Active-Org-ID instead of the client-level OrganizationId.
    /// This property is NOT serialised into the JSON request body.
    /// </summary>
    [JsonIgnore]
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Request body for triggering an on-demand backup.
/// </summary>
public class CreateBackupRequest
{
    /// <summary>Backup type. Defaults to Full when omitted.</summary>
    [JsonPropertyName("backup_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackupType? BackupType { get; set; }

    /// <summary>Optional human-readable label.</summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }
}

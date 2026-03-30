using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// A managed database service (cluster).
/// </summary>
public class Service
{
    /// <summary>Unique service identifier (UUID).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Current lifecycle state.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Database engine type.</summary>
    [JsonPropertyName("database_type")]
    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>Database engine version string (e.g. "17", "8.4").</summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>Compute plan name (e.g. "tier-2").</summary>
    [JsonPropertyName("plan_name")]
    public string? PlanName { get; set; }

    /// <summary>UpCloud zone (e.g. "se-sto1").</summary>
    [JsonPropertyName("zone")]
    public string? Zone { get; set; }

    /// <summary>Data disk size in GB.</summary>
    [JsonPropertyName("storage_size_gb")]
    public int? StorageSizeGb { get; set; }

    /// <summary>Storage tier ("standard" or "maxiops").</summary>
    [JsonPropertyName("storage_tier")]
    public string? StorageTier { get; set; }

    /// <summary>Number of nodes in the cluster.</summary>
    [JsonPropertyName("node_count")]
    public int? NodeCount { get; set; }

    /// <summary>Whether automated failover is enabled.</summary>
    [JsonPropertyName("auto_failover_enabled")]
    public bool? AutoFailoverEnabled { get; set; }

    /// <summary>Whether encryption at rest is enabled.</summary>
    [JsonPropertyName("encryption_enabled")]
    public bool? EncryptionEnabled { get; set; }

    /// <summary>Allowed CIDR blocks for firewall access.</summary>
    [JsonPropertyName("allowed_cidrs")]
    public List<string>? AllowedCidrs { get; set; }

    /// <summary>DNS records exposed to clients.</summary>
    [JsonPropertyName("dns_records")]
    public List<DnsRecord>? DnsRecords { get; set; }

    /// <summary>Organisation this service belongs to.</summary>
    [JsonPropertyName("organization_id")]
    public string? OrganizationId { get; set; }

    /// <summary>ISO-8601 timestamp when the service was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>ISO-8601 timestamp of the last update.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// A DNS record associated with a service.
/// </summary>
public class DnsRecord
{
    [JsonPropertyName("full_domain")]
    public string? FullDomain { get; set; }

    [JsonPropertyName("record_type")]
    public string? RecordType { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

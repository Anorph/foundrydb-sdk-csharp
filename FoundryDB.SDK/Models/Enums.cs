using System.Text.Json.Serialization;

namespace FoundryDB.SDK.Models;

/// <summary>
/// Supported database engine types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DatabaseType
{
    [JsonPropertyName("postgresql")]
    PostgreSQL,

    [JsonPropertyName("mysql")]
    MySQL,

    [JsonPropertyName("mongodb")]
    MongoDB,

    [JsonPropertyName("valkey")]
    Valkey,

    [JsonPropertyName("kafka")]
    Kafka,

    [JsonPropertyName("opensearch")]
    OpenSearch,

    [JsonPropertyName("mssql")]
    MSSQL
}

/// <summary>
/// High-level lifecycle state of a managed service.
/// Values match the lowercase strings returned by the FoundryDB API.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceStatus
{
    [JsonPropertyName("pending")]
    Pending,

    [JsonPropertyName("provisioning")]
    Provisioning,

    [JsonPropertyName("running")]
    Running,

    [JsonPropertyName("stopped")]
    Stopped,

    [JsonPropertyName("error")]
    Error,

    [JsonPropertyName("deleting")]
    Deleting,

    [JsonPropertyName("deleted")]
    Deleted
}

/// <summary>
/// Backup state.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupStatus
{
    [JsonPropertyName("pending")]
    Pending,

    [JsonPropertyName("running")]
    Running,

    [JsonPropertyName("completed")]
    Completed,

    [JsonPropertyName("failed")]
    Failed
}

/// <summary>
/// Backup type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupType
{
    [JsonPropertyName("full")]
    Full,

    [JsonPropertyName("incremental")]
    Incremental,

    [JsonPropertyName("pitr")]
    PITR
}

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
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceStatus
{
    [JsonPropertyName("Pending")]
    Pending,

    [JsonPropertyName("SetupNetwork")]
    SetupNetwork,

    [JsonPropertyName("SetupServer")]
    SetupServer,

    [JsonPropertyName("SetupAgent")]
    SetupAgent,

    [JsonPropertyName("SetupDNS")]
    SetupDNS,

    [JsonPropertyName("SetupLB")]
    SetupLB,

    [JsonPropertyName("Running")]
    Running,

    [JsonPropertyName("Stopped")]
    Stopped,

    [JsonPropertyName("Restarting")]
    Restarting,

    [JsonPropertyName("Checkup")]
    Checkup,

    [JsonPropertyName("Upgrading")]
    Upgrading,

    [JsonPropertyName("Scaling")]
    Scaling,

    [JsonPropertyName("DeleteService")]
    DeleteService,

    [JsonPropertyName("DeleteDNS")]
    DeleteDNS,

    [JsonPropertyName("DeleteServer")]
    DeleteServer,

    [JsonPropertyName("DeleteNetwork")]
    DeleteNetwork,

    [JsonPropertyName("Failed")]
    Failed
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
    Incremental
}

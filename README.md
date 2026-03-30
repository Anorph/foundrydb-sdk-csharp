# FoundryDB C# SDK

Official C# client library for the [FoundryDB](https://foundrydb.com) managed database platform.

Provision, monitor, and manage PostgreSQL, MySQL, MongoDB, Valkey, Kafka, OpenSearch, and MSSQL clusters from any .NET application.

## Requirements

- .NET 8.0 or later
- No third-party dependencies (uses only `System.Net.Http` and `System.Text.Json` from the BCL)

## Installation

```bash
# NuGet (coming soon)
dotnet add package FoundryDB.SDK
```

Until the package is published to NuGet, reference it directly:

```bash
# Clone and reference locally
git clone https://github.com/Anorph/foundrydb-sdk-csharp.git
dotnet add reference ../foundrydb-sdk-csharp/FoundryDB.SDK/FoundryDB.SDK.csproj
```

## Quick Start

```csharp
using FoundryDB.SDK;
using FoundryDB.SDK.Models;

// Create a client using Basic auth
using var client = new FoundryDBClient(new FoundryDBConfig
{
    ApiUrl   = "https://api.foundrydb.com",
    Username = "admin",
    Password = "yourpassword"
});

// List all services
var services = await client.ListServicesAsync();
foreach (var svc in services)
    Console.WriteLine($"{svc.Name}  [{svc.Status}]  {svc.DatabaseType}");

// Create a PostgreSQL 17 service
var service = await client.CreateServiceAsync(new CreateServiceRequest
{
    Name          = "my-pg",
    DatabaseType  = DatabaseType.PostgreSQL,
    Version       = "17",
    PlanName      = "tier-2",
    Zone          = "se-sto1",
    StorageSizeGb = 50,
    StorageTier   = "maxiops"
});

// Wait until it is Running (polls every 10 s, 15-min default timeout)
var running = await client.WaitForRunningAsync(service.Id);
Console.WriteLine($"Endpoint: {running.DnsRecords?[0].FullDomain}");
```

## Authentication

The SDK supports two authentication modes.

### HTTP Basic

```csharp
new FoundryDBConfig
{
    ApiUrl   = "https://api.foundrydb.com",
    Username = "admin",
    Password = "yourpassword"
}
```

### Bearer Token

```csharp
new FoundryDBConfig
{
    ApiUrl = "https://api.foundrydb.com",
    Token  = "your-bearer-token"
}
```

When `Token` is set it takes precedence over `Username`/`Password`.

## Multi-Tenant (Organisations)

Pass a default organisation ID in the config, or override it per request:

```csharp
// Client-level default
var client = new FoundryDBClient(new FoundryDBConfig
{
    ...
    OrganizationId = "org-uuid"
});

// Per-request override
await client.CreateServiceAsync(new CreateServiceRequest
{
    ...
    OrganizationId = "other-org-uuid"   // overrides the client default
});
```

The SDK sends the active organisation as the `X-Active-Org-ID` HTTP header.

## Configuration Reference

| Property | Type | Required | Description |
|---|---|---|---|
| `ApiUrl` | `string` | Yes | Base URL of the FoundryDB API |
| `Username` | `string` | One of Username/Token | HTTP Basic username |
| `Password` | `string` | One of Password/Token | HTTP Basic password |
| `Token` | `string` | One of Username/Token | Bearer token (takes precedence) |
| `OrganizationId` | `string` | No | Default org sent as `X-Active-Org-ID` |

## API Reference

### Services

```csharp
// List all services
List<Service> services = await client.ListServicesAsync(ct);

// Get a specific service
Service svc = await client.GetServiceAsync("service-uuid", ct);

// Create a service
Service svc = await client.CreateServiceAsync(new CreateServiceRequest { ... }, ct);

// Delete a service
await client.DeleteServiceAsync("service-uuid", ct);

// Wait until Running
Service svc = await client.WaitForRunningAsync("service-uuid", timeout: TimeSpan.FromMinutes(20), ct);
```

The sub-resource API is also available directly:

```csharp
client.Services.ListAsync()
client.Services.GetAsync(id)
client.Services.CreateAsync(req)
client.Services.DeleteAsync(id)
client.Services.WaitForRunningAsync(id, timeout, ct)
```

### CreateServiceRequest

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Display name. Required. |
| `DatabaseType` | `DatabaseType` | Engine type. Required. |
| `Version` | `string?` | Major version (e.g. `"17"`, `"8.4"`). Defaults to latest. |
| `PlanName` | `string` | Compute plan (e.g. `"tier-2"`). Required. |
| `Zone` | `string?` | UpCloud zone slug. Defaults to `"se-sto1"`. |
| `StorageSizeGb` | `int?` | Data disk size in GB. |
| `StorageTier` | `string?` | `"standard"` or `"maxiops"` (NVMe). |
| `NodeCount` | `int?` | Nodes in the cluster (for HA setups). |
| `AutoFailoverEnabled` | `bool?` | Enable automated failover. |
| `ReplicationMode` | `string?` | `"sync"` or `"async"`. |
| `EncryptionEnabled` | `bool?` | Encryption at rest. |
| `AllowedCidrs` | `List<string>?` | Firewall allowed CIDR blocks. |
| `OrganizationId` | `string?` | Per-request org override (not serialised to JSON). |

### Organizations

```csharp
List<Organization> orgs = await client.ListOrganizationsAsync(ct);

// Sub-resource accessor
client.Organizations.ListAsync(ct)
```

### Users

```csharp
// List database users for a service
List<DatabaseUser> users = await client.ListUsersAsync("service-uuid", ct);

// Reveal plaintext password
string password = await client.RevealPasswordAsync("service-uuid", "username", ct);

// Sub-resource accessor
client.Users.ListAsync(serviceId, ct)
client.Users.RevealPasswordAsync(serviceId, username, ct)
```

### Backups

```csharp
// List backups
List<Backup> backups = await client.ListBackupsAsync("service-uuid", ct);

// Trigger an on-demand backup
Backup backup = await client.TriggerBackupAsync("service-uuid", new CreateBackupRequest
{
    BackupType = BackupType.Full
}, ct);

// Sub-resource accessor
client.Backups.ListAsync(serviceId, ct)
client.Backups.TriggerAsync(serviceId, req, ct)
```

## Error Handling

All API errors throw `FoundryDBException`:

```csharp
try
{
    var svc = await client.GetServiceAsync("nonexistent-id");
}
catch (FoundryDBException ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");
    Console.WriteLine($"Title:  {ex.Title}");
    Console.WriteLine($"Detail: {ex.Detail}");
}
catch (TimeoutException ex)
{
    // Thrown by WaitForRunningAsync when the deadline is exceeded
    Console.WriteLine(ex.Message);
}
```

`FoundryDBException` properties:

| Property | Type | Description |
|---|---|---|
| `StatusCode` | `int` | HTTP status code (e.g. 404, 422, 500) |
| `Title` | `string` | Short error title |
| `Detail` | `string` | Full error message from the API |

## Supported Database Versions

| Engine | `DatabaseType` enum | Supported Versions |
|---|---|---|
| PostgreSQL | `DatabaseType.PostgreSQL` | 14, 15, 16, 17 |
| MySQL | `DatabaseType.MySQL` | 8.0, 8.4 |
| MongoDB | `DatabaseType.MongoDB` | 6.0, 7.0, 8.0 |
| Valkey | `DatabaseType.Valkey` | 7.2, 8.0, 8.1 |
| Kafka | `DatabaseType.Kafka` | 3.7, 3.8, 3.9 |
| OpenSearch | `DatabaseType.OpenSearch` | 2.x |
| MSSQL | `DatabaseType.MSSQL` | 2022 |

## Compute Plans

Plans specify CPU and memory only. Storage is configured separately.

| Plan | CPU | Memory |
|---|---|---|
| `tier-1` | 1 vCPU | 2 GB |
| `tier-2` | 2 vCPU | 4 GB |
| `tier-4` | 4 vCPU | 8 GB |
| `tier-8` | 8 vCPU | 16 GB |
| `tier-16` | 16 vCPU | 32 GB |

Use `tier-2` for development and testing.

## Advanced Usage

### Custom HttpClient

Pass your own `HttpClient` for custom retry policies, proxies, or test mocking:

```csharp
var handler = new HttpClientHandler { ... };
var httpClient = new HttpClient(handler);

using var client = new FoundryDBClient(config, httpClient);
```

### Cancellation

Every method accepts an optional `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var services = await client.ListServicesAsync(cts.Token);
```

### Sub-resource APIs

For cleaner code, use the sub-resource accessors directly:

```csharp
var svc    = await client.Services.CreateAsync(req, ct);
var users  = await client.Users.ListAsync(svc.Id, ct);
var backup = await client.Backups.TriggerAsync(svc.Id, new CreateBackupRequest(), ct);
```

## Testing

```bash
dotnet test FoundryDB.SDK.Tests/
```

The test suite uses xUnit with a custom `MockHttpHandler` to intercept HTTP calls without making real network requests. Tests cover all public methods across `FoundryDBClient`, `ServicesApi`, `OrganizationsApi`, `UsersApi`, `BackupsApi`, and `FoundryDBException`, including success paths, error paths, auth header assertions, and `WaitForRunningAsync` polling behaviour.

> Note: `dotnet` must be installed (net8.0 SDK or later). It is not bundled with this repository.

## Running the Example

```bash
cd examples/BasicExample

export FOUNDRYDB_URL=https://api.foundrydb.com
export FOUNDRYDB_USERNAME=admin
export FOUNDRYDB_PASSWORD=yourpassword
# Optional:
export FOUNDRYDB_ORG_ID=your-org-uuid

dotnet run
```

## License

MIT License. See [LICENSE](LICENSE) for details.

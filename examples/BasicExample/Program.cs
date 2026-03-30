using FoundryDB.SDK;
using FoundryDB.SDK.Models;

// ---------------------------------------------------------------------------
// FoundryDB C# SDK - Basic Example
//
// This example demonstrates the most common operations:
//   1. Connecting to the API
//   2. Listing existing services
//   3. Creating a new PostgreSQL service
//   4. Waiting for it to reach the Running state
//   5. Listing database users and revealing a password
//   6. Triggering an on-demand backup
//   7. Deleting the service when done
//
// Set these environment variables before running:
//   FOUNDRYDB_URL       - API base URL  (default: https://api.foundrydb.com)
//   FOUNDRYDB_USERNAME  - Admin username
//   FOUNDRYDB_PASSWORD  - Admin password
//   FOUNDRYDB_ORG_ID    - (optional) Organisation UUID for multi-tenant setups
// ---------------------------------------------------------------------------

var apiUrl   = Environment.GetEnvironmentVariable("FOUNDRYDB_URL")      ?? "https://api.foundrydb.com";
var username = Environment.GetEnvironmentVariable("FOUNDRYDB_USERNAME")  ?? "admin";
var password = Environment.GetEnvironmentVariable("FOUNDRYDB_PASSWORD")  ?? "admin";
var orgId    = Environment.GetEnvironmentVariable("FOUNDRYDB_ORG_ID");

using var client = new FoundryDBClient(new FoundryDBConfig
{
    ApiUrl         = apiUrl,
    Username       = username,
    Password       = password,
    OrganizationId = orgId
});

Console.WriteLine("=== FoundryDB C# SDK - Basic Example ===");
Console.WriteLine($"API: {apiUrl}");
Console.WriteLine();

// 1. List organisations
Console.WriteLine(">> Listing organisations...");
var orgs = await client.ListOrganizationsAsync();
foreach (var org in orgs)
    Console.WriteLine($"   {org.Id}  {org.Name}");
Console.WriteLine();

// 2. List existing services
Console.WriteLine(">> Listing existing services...");
var services = await client.ListServicesAsync();
foreach (var svc in services)
    Console.WriteLine($"   {svc.Id}  {svc.Name}  [{svc.Status}]  {svc.DatabaseType}");
Console.WriteLine($"   Total: {services.Count}");
Console.WriteLine();

// 3. Create a new PostgreSQL service
Console.WriteLine(">> Creating a PostgreSQL 17 service (tier-2, se-sto1)...");
var storageSizeGb = 50;
var newService = await client.CreateServiceAsync(new CreateServiceRequest
{
    Name           = "example-pg-csharp",
    DatabaseType   = DatabaseType.PostgreSQL,
    Version        = "17",
    PlanName       = "tier-2",
    Zone           = "se-sto1",
    StorageSizeGb  = storageSizeGb,
    StorageTier    = "maxiops",
    AllowedCidrs   = new List<string> { "0.0.0.0/0" },
    OrganizationId = orgId   // per-request override (optional)
});

Console.WriteLine($"   Created: {newService.Id}  status={newService.Status}");
Console.WriteLine();

// 4. Wait for the service to be Running (up to 20 minutes)
Console.WriteLine(">> Waiting for service to reach Running state (up to 20 min)...");
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Service runningService;
try
{
    runningService = await client.WaitForRunningAsync(
        newService.Id,
        timeout: TimeSpan.FromMinutes(20),
        ct: cts.Token);
}
catch (TimeoutException ex)
{
    Console.WriteLine($"   Timed out: {ex.Message}");
    return;
}
catch (FoundryDBException ex)
{
    Console.WriteLine($"   API error [{ex.StatusCode}]: {ex.Detail}");
    return;
}

Console.WriteLine($"   Service is Running!");
if (runningService.DnsRecords is { Count: > 0 })
    Console.WriteLine($"   Endpoint: {runningService.DnsRecords[0].FullDomain}");
Console.WriteLine();

// 5. List database users and reveal the admin password
Console.WriteLine(">> Listing database users...");
var users = await client.ListUsersAsync(newService.Id);
foreach (var u in users)
    Console.WriteLine($"   {u.Username}  admin={u.IsAdmin}");

if (users.Count > 0)
{
    var adminUser = users.FirstOrDefault(u => u.IsAdmin == true) ?? users[0];
    Console.WriteLine($">> Revealing password for '{adminUser.Username}'...");
    var password2 = await client.RevealPasswordAsync(newService.Id, adminUser.Username);
    Console.WriteLine($"   Password: {password2}");
}
Console.WriteLine();

// 6. Trigger an on-demand backup
Console.WriteLine(">> Triggering a full backup...");
var backup = await client.TriggerBackupAsync(newService.Id, new CreateBackupRequest
{
    BackupType = BackupType.Full
});
Console.WriteLine($"   Backup triggered: {backup.Id}  status={backup.Status}");
Console.WriteLine();

// 7. List backups
Console.WriteLine(">> Listing backups...");
var backups = await client.ListBackupsAsync(newService.Id);
foreach (var b in backups)
    Console.WriteLine($"   {b.Id}  {b.BackupType}  {b.Status}  created={b.CreatedAt:u}");
Console.WriteLine();

// 8. Clean up - delete the test service
Console.Write(">> Delete the test service? [y/N] ");
var answer = Console.ReadLine();
if (string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
{
    await client.DeleteServiceAsync(newService.Id);
    Console.WriteLine("   Deletion initiated.");
}
else
{
    Console.WriteLine($"   Skipped. Delete manually: DELETE /managed-services/{newService.Id}");
}

Console.WriteLine();
Console.WriteLine("Done.");

using System.Text.Json;
using System.Text.Json.Serialization;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Backups;

/// <summary>
/// Operations on backups for a managed service.
/// </summary>
public class BackupsApi
{
    private readonly FoundryDBClient _client;

    internal BackupsApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Returns all backups for a service, newest first.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    public async Task<List<Backup>> ListAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.GetAsync($"/managed-services/{serviceId}/backups", orgId: null, ct).ConfigureAwait(false);
        var backups = new List<Backup>();

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var b = JsonSerializer.Deserialize<Backup>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (b is not null) backups.Add(b);
            }
        }
        else if (doc.RootElement.TryGetProperty("backups", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var b = JsonSerializer.Deserialize<Backup>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (b is not null) backups.Add(b);
            }
        }

        return backups;
    }

    /// <summary>
    /// Triggers an on-demand backup for a service.
    /// The API returns a <c>backup_id</c> field in the response envelope; this method
    /// normalises that into a <see cref="Backup"/> object consistent with <see cref="ListAsync"/>.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    /// <param name="req">Backup parameters (type, optional label).</param>
    public async Task<Backup> TriggerAsync(string serviceId, CreateBackupRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));
        ArgumentNullException.ThrowIfNull(req);

        var json = await _client.PostAsync($"/managed-services/{serviceId}/backups", req, orgId: null, ct).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(json);

        // Guard against a null or non-object root (e.g. JSON literal "null").
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a backup object.");

        // Primary response format: { "backup_id": "...", "status": "...", "message": "...", "task_id": "..." }
        if (doc.RootElement.TryGetProperty("backup_id", out var backupIdEl))
        {
            var backupId = backupIdEl.GetString() ?? string.Empty;
            var status = doc.RootElement.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString() ?? string.Empty
                : string.Empty;
            return new Backup
            {
                Id = backupId,
                ServiceId = serviceId,
                Status = status,
                BackupType = req.BackupType.HasValue ? req.BackupType.Value.ToString().ToLowerInvariant() : null
            };
        }

        // Secondary format: { "backup": { ... } } wrapper
        if (doc.RootElement.TryGetProperty("backup", out var el))
        {
            var b = JsonSerializer.Deserialize<Backup>(el.GetRawText(), FoundryDBClient.JsonOptions);
            if (b is not null) return b;
        }

        // Fallback: entire response is the backup object.
        var backup = JsonSerializer.Deserialize<Backup>(json, FoundryDBClient.JsonOptions);
        return backup ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a backup object.");
    }
}

using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Users;

/// <summary>
/// Operations on database-level users for a managed service.
/// </summary>
public class UsersApi
{
    private readonly FoundryDBClient _client;

    internal UsersApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Returns all database users for a service.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    public async Task<List<DatabaseUser>> ListAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));

        var json = await _client.GetAsync($"/managed-services/{serviceId}/database-users", orgId: null, ct).ConfigureAwait(false);
        var users = new List<DatabaseUser>();

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var u = JsonSerializer.Deserialize<DatabaseUser>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (u is not null) users.Add(u);
            }
        }
        else if (doc.RootElement.TryGetProperty("users", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var u = JsonSerializer.Deserialize<DatabaseUser>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (u is not null) users.Add(u);
            }
        }

        return users;
    }

    /// <summary>
    /// Reveals the full connection credentials (including plaintext password) for a database user.
    /// </summary>
    /// <param name="serviceId">Service UUID.</param>
    /// <param name="username">Database username.</param>
    /// <returns>Full connection credentials including host, port, database, and connection string.</returns>
    public async Task<RevealPasswordResponse> RevealPasswordAsync(string serviceId, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("Service ID must not be empty.", nameof(serviceId));
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username must not be empty.", nameof(username));

        var json = await _client.PostAsync(
            $"/managed-services/{serviceId}/database-users/{username}/reveal-password",
            payload: null,
            orgId: null,
            ct).ConfigureAwait(false);

        var creds = JsonSerializer.Deserialize<RevealPasswordResponse>(json, FoundryDBClient.JsonOptions);
        if (creds is null || string.IsNullOrEmpty(creds.Password))
            throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a password field.");

        return creds;
    }
}

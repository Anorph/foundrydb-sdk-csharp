using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Organizations;

/// <summary>
/// Operations on organisations (tenants).
/// </summary>
public class OrganizationsApi
{
    private readonly FoundryDBClient _client;

    internal OrganizationsApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Returns all organisations the authenticated user belongs to.
    /// </summary>
    public async Task<List<Organization>> ListAsync(CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/organizations", orgId: null, ct).ConfigureAwait(false);
        var orgs = new List<Organization>();

        using var doc = JsonDocument.Parse(json);

        // The response may be a JSON array at the root, or wrapped in an "organizations" key.
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var org = JsonSerializer.Deserialize<Organization>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (org is not null) orgs.Add(org);
            }
        }
        else if (doc.RootElement.TryGetProperty("organizations", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var org = JsonSerializer.Deserialize<Organization>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (org is not null) orgs.Add(org);
            }
        }

        return orgs;
    }
}

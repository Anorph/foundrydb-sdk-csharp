using System.Text.Json;
using FoundryDB.SDK.Models;

namespace FoundryDB.SDK.Services;

/// <summary>
/// Operations on managed database services.
/// </summary>
public class ServicesApi
{
    private readonly FoundryDBClient _client;

    internal ServicesApi(FoundryDBClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Returns all services visible to the authenticated user (optionally scoped to an organisation).
    /// </summary>
    public async Task<List<Service>> ListAsync(CancellationToken ct = default)
    {
        var json = await _client.GetAsync("/managed-services", orgId: null, ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);
        var services = new List<Service>();

        if (doc.RootElement.TryGetProperty("services", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var svc = JsonSerializer.Deserialize<Service>(el.GetRawText(), FoundryDBClient.JsonOptions);
                if (svc is not null) services.Add(svc);
            }
        }

        return services;
    }

    /// <summary>
    /// Returns a single service by ID.
    /// </summary>
    /// <param name="id">Service UUID.</param>
    public async Task<Service> GetAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Service ID must not be empty.", nameof(id));

        var json = await _client.GetAsync($"/managed-services/{id}", orgId: null, ct).ConfigureAwait(false);
        return Deserialize<Service>(json, "service") ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a service object.");
    }

    /// <summary>
    /// Provisions a new managed database service.
    /// </summary>
    /// <param name="req">Creation parameters.</param>
    public async Task<Service> CreateAsync(CreateServiceRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Service name must not be empty.", nameof(req));
        if (string.IsNullOrWhiteSpace(req.PlanName)) throw new ArgumentException("PlanName must not be empty.", nameof(req));

        var orgId = req.OrganizationId ?? _client.Config.OrganizationId;
        var json = await _client.PostAsync("/managed-services", req, orgId, ct).ConfigureAwait(false);
        return Deserialize<Service>(json, "service") ?? throw new FoundryDBException(200, "Deserialization Error", "Response did not contain a service object.");
    }

    /// <summary>
    /// Deletes a service by ID and all its associated resources.
    /// </summary>
    /// <param name="id">Service UUID.</param>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Service ID must not be empty.", nameof(id));

        await _client.DeleteAsync($"/managed-services/{id}", orgId: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls the service until its status is "Running", then returns it.
    /// Throws <see cref="TimeoutException"/> if the service does not reach Running within the timeout.
    /// </summary>
    /// <param name="id">Service UUID.</param>
    /// <param name="timeout">Maximum wait duration. Defaults to 15 minutes.</param>
    public async Task<Service> WaitForRunningAsync(string id, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Service ID must not be empty.", nameof(id));

        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromMinutes(15));
        var pollInterval = TimeSpan.FromSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var svc = await GetAsync(id, ct).ConfigureAwait(false);

            if (string.Equals(svc.Status, "Running", StringComparison.OrdinalIgnoreCase))
                return svc;

            // Surface terminal failure states immediately rather than waiting out the full timeout.
            if (string.Equals(svc.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                throw new FoundryDBException(0, "Service Failed", $"Service '{id}' transitioned to Failed state.");

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) break;

            var delay = remaining < pollInterval ? remaining : pollInterval;
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"Service '{id}' did not reach Running state within {timeout ?? TimeSpan.FromMinutes(15)}.");
    }

    // ----- helpers -----

    private static T? Deserialize<T>(string json, string? rootProperty = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        if (rootProperty is null)
            return JsonSerializer.Deserialize<T>(json, FoundryDBClient.JsonOptions);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(rootProperty, out var el))
            return JsonSerializer.Deserialize<T>(el.GetRawText(), FoundryDBClient.JsonOptions);

        // Fallback: attempt to deserialise the root element directly.
        return JsonSerializer.Deserialize<T>(json, FoundryDBClient.JsonOptions);
    }
}

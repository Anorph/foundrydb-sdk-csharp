using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoundryDB.SDK.Backups;
using FoundryDB.SDK.Models;
using FoundryDB.SDK.Organizations;
using FoundryDB.SDK.Services;
using FoundryDB.SDK.Users;

namespace FoundryDB.SDK;

/// <summary>
/// Configuration for <see cref="FoundryDBClient"/>.
/// Provide either Username + Password for HTTP Basic auth, or Token for Bearer auth.
/// </summary>
public class FoundryDBConfig
{
    /// <summary>Base URL of the FoundryDB controller API (e.g. "https://api.foundrydb.com").</summary>
    public string ApiUrl { get; set; } = "https://api.foundrydb.com";

    /// <summary>Username for HTTP Basic authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Password for HTTP Basic authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Bearer token for token-based authentication (takes precedence over Basic).</summary>
    public string? Token { get; set; }

    /// <summary>
    /// Default organisation ID sent in X-Active-Org-ID for every request.
    /// Can be overridden per-request via <see cref="CreateServiceRequest.OrganizationId"/>.
    /// </summary>
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Client for the FoundryDB managed database platform API.
/// </summary>
public class FoundryDBClient : IDisposable
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private bool _disposed;

    /// <summary>Configuration used by this client instance.</summary>
    public FoundryDBConfig Config { get; }

    // ----- Sub-resource accessors -----

    /// <summary>Operations on managed database services.</summary>
    public ServicesApi Services { get; }

    /// <summary>Operations on organisations.</summary>
    public OrganizationsApi Organizations { get; }

    /// <summary>Operations on database-level users.</summary>
    public UsersApi Users { get; }

    /// <summary>Operations on backups.</summary>
    public BackupsApi Backups { get; }

    // ----- Constructors -----

    /// <summary>
    /// Creates a new <see cref="FoundryDBClient"/> with the given configuration.
    /// </summary>
    public FoundryDBClient(FoundryDBConfig config) : this(config, null) { }

    /// <summary>
    /// Creates a new <see cref="FoundryDBClient"/> with a custom <see cref="HttpClient"/>.
    /// Useful for testing and advanced scenarios (proxy, retry handlers, etc.).
    /// </summary>
    public FoundryDBClient(FoundryDBConfig config, HttpClient? httpClient)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.ApiUrl))
            throw new ArgumentException("ApiUrl must not be empty.", nameof(config));

        if (string.IsNullOrWhiteSpace(config.Token) &&
            (string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password)))
        {
            throw new ArgumentException(
                "Provide either Token or both Username and Password in FoundryDBConfig.", nameof(config));
        }

        _http = httpClient ?? new HttpClient();
        _http.BaseAddress = new Uri(config.ApiUrl.TrimEnd('/'));
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(config.Token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.Token);
        }
        else
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        Services = new ServicesApi(this);
        Organizations = new OrganizationsApi(this);
        Users = new UsersApi(this);
        Backups = new BackupsApi(this);
    }

    // ----- Convenience top-level methods -----

    /// <summary>Lists all managed services. Shorthand for <c>Services.ListAsync()</c>.</summary>
    public Task<List<Service>> ListServicesAsync(CancellationToken ct = default)
        => Services.ListAsync(ct);

    /// <summary>Gets a service by ID. Shorthand for <c>Services.GetAsync()</c>.</summary>
    public Task<Service> GetServiceAsync(string id, CancellationToken ct = default)
        => Services.GetAsync(id, ct);

    /// <summary>Creates a service. Shorthand for <c>Services.CreateAsync()</c>.</summary>
    public Task<Service> CreateServiceAsync(CreateServiceRequest req, CancellationToken ct = default)
        => Services.CreateAsync(req, ct);

    /// <summary>Deletes a service. Shorthand for <c>Services.DeleteAsync()</c>.</summary>
    public Task DeleteServiceAsync(string id, CancellationToken ct = default)
        => Services.DeleteAsync(id, ct);

    /// <summary>Waits until a service is Running. Shorthand for <c>Services.WaitForRunningAsync()</c>.</summary>
    public Task<Service> WaitForRunningAsync(string id, TimeSpan? timeout = null, CancellationToken ct = default)
        => Services.WaitForRunningAsync(id, timeout, ct);

    /// <summary>Lists organisations. Shorthand for <c>Organizations.ListAsync()</c>.</summary>
    public Task<List<Organization>> ListOrganizationsAsync(CancellationToken ct = default)
        => Organizations.ListAsync(ct);

    /// <summary>Lists database users. Shorthand for <c>Users.ListAsync()</c>.</summary>
    public Task<List<DatabaseUser>> ListUsersAsync(string serviceId, CancellationToken ct = default)
        => Users.ListAsync(serviceId, ct);

    /// <summary>Reveals a user's password. Shorthand for <c>Users.RevealPasswordAsync()</c>.</summary>
    public Task<string> RevealPasswordAsync(string serviceId, string username, CancellationToken ct = default)
        => Users.RevealPasswordAsync(serviceId, username, ct);

    /// <summary>Lists backups. Shorthand for <c>Backups.ListAsync()</c>.</summary>
    public Task<List<Backup>> ListBackupsAsync(string serviceId, CancellationToken ct = default)
        => Backups.ListAsync(serviceId, ct);

    /// <summary>Triggers a backup. Shorthand for <c>Backups.TriggerAsync()</c>.</summary>
    public Task<Backup> TriggerBackupAsync(string serviceId, CreateBackupRequest req, CancellationToken ct = default)
        => Backups.TriggerAsync(serviceId, req, ct);

    // ----- Internal HTTP helpers -----

    internal async Task<string> GetAsync(string path, string? orgId, CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Get, path, orgId);
        return await SendAsync(req, ct).ConfigureAwait(false);
    }

    internal async Task<string> PostAsync(string path, object? payload, string? orgId, CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Post, path, orgId);
        if (payload is not null)
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions),
                Encoding.UTF8,
                "application/json");
        else
            req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        return await SendAsync(req, ct).ConfigureAwait(false);
    }

    internal async Task DeleteAsync(string path, string? orgId, CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Delete, path, orgId);
        await SendAsync(req, ct).ConfigureAwait(false);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, string? orgId)
    {
        var req = new HttpRequestMessage(method, path);

        var effectiveOrgId = orgId ?? Config.OrganizationId;
        if (!string.IsNullOrWhiteSpace(effectiveOrgId))
            req.Headers.Add("X-Active-Org-ID", effectiveOrgId);

        return req;
    }

    private async Task<string> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);

        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            ThrowApiError((int)resp.StatusCode, body);

        return body;
    }

    private static void ThrowApiError(int statusCode, string body)
    {
        string title = "API Error";
        string detail = body;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errEl) && errEl.ValueKind == JsonValueKind.String)
                    detail = errEl.GetString() ?? body;
                else if (root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                    detail = msgEl.GetString() ?? body;
                else if (root.TryGetProperty("detail", out var detEl) && detEl.ValueKind == JsonValueKind.String)
                    detail = detEl.GetString() ?? body;

                if (root.TryGetProperty("title", out var titEl) && titEl.ValueKind == JsonValueKind.String)
                    title = titEl.GetString() ?? title;
            }
            catch (JsonException)
            {
                // Body is not JSON - use as-is for detail.
            }
        }

        throw new FoundryDBException(statusCode, title, detail);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

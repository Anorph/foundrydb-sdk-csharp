using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
using Xunit;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Tests for <see cref="FoundryDBClient"/> construction, configuration, auth headers,
/// the X-Active-Org-ID header, top-level shorthand methods, error parsing, and Dispose.
/// </summary>
public class ClientTests
{
    // ----- Construction / validation -----

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FoundryDBClient(null!));
    }

    [Fact]
    public void Constructor_EmptyApiUrl_ThrowsArgumentException()
    {
        var cfg = new FoundryDBConfig { ApiUrl = " ", Token = "tok" };
        Assert.Throws<ArgumentException>(() => new FoundryDBClient(cfg));
    }

    [Fact]
    public void Constructor_NoCredentials_ThrowsArgumentException()
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com" };
        Assert.Throws<ArgumentException>(() => new FoundryDBClient(cfg));
    }

    [Fact]
    public void Constructor_UsernameWithoutPassword_ThrowsArgumentException()
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Username = "user" };
        Assert.Throws<ArgumentException>(() => new FoundryDBClient(cfg));
    }

    [Fact]
    public void Constructor_TokenAuth_SetsConfigAndSubApis()
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "mytoken" };
        using var client = new FoundryDBClient(cfg);

        Assert.Same(cfg, client.Config);
        Assert.NotNull(client.Services);
        Assert.NotNull(client.Organizations);
        Assert.NotNull(client.Users);
        Assert.NotNull(client.Backups);
    }

    [Fact]
    public void Constructor_BasicAuth_SetsConfigAndSubApis()
    {
        var cfg = new FoundryDBConfig
        {
            ApiUrl = "https://api.foundrydb.com",
            Username = "admin",
            Password = "secret"
        };
        using var client = new FoundryDBClient(cfg);

        Assert.NotNull(client.Config);
        Assert.NotNull(client.Services);
        Assert.NotNull(client.Organizations);
        Assert.NotNull(client.Users);
        Assert.NotNull(client.Backups);
    }

    // ----- Auth header - Bearer -----

    [Fact]
    public async Task BearerToken_IsSentInAuthorizationHeader()
    {
        AuthenticationHeaderValue? captured = null;
        var handler = new MockHttpHandler(req =>
        {
            captured = req.Headers.Authorization;
            return Responses.Ok("{\"services\":[]}");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "my-bearer-token" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListServicesAsync();

        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Scheme);
        Assert.Equal("my-bearer-token", captured.Parameter);
    }

    // ----- Auth header - Basic -----

    [Fact]
    public async Task BasicAuth_IsSentInAuthorizationHeader()
    {
        AuthenticationHeaderValue? captured = null;
        var handler = new MockHttpHandler(req =>
        {
            captured = req.Headers.Authorization;
            return Responses.Ok("{\"services\":[]}");
        });

        var cfg = new FoundryDBConfig
        {
            ApiUrl = "https://api.foundrydb.com",
            Username = "admin",
            Password = "hunter2"
        };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListServicesAsync();

        Assert.NotNull(captured);
        Assert.Equal("Basic", captured!.Scheme);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(captured.Parameter!));
        Assert.Equal("admin:hunter2", decoded);
    }

    // ----- X-Active-Org-ID -----

    [Fact]
    public async Task OrgId_FromConfig_IsSentAsHeader()
    {
        string? orgHeader = null;
        var handler = new MockHttpHandler(req =>
        {
            req.Headers.TryGetValues("X-Active-Org-ID", out var vals);
            orgHeader = vals is not null ? string.Join(",", vals) : null;
            return Responses.Ok("{\"services\":[]}");
        });

        var cfg = new FoundryDBConfig
        {
            ApiUrl = "https://api.foundrydb.com",
            Token = "tok",
            OrganizationId = "org-from-config"
        };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListServicesAsync();

        Assert.Equal("org-from-config", orgHeader);
    }

    [Fact]
    public async Task OrgId_OnCreateServiceRequest_OverridesConfigOrgId()
    {
        string? orgHeader = null;
        var handler = new MockHttpHandler(req =>
        {
            req.Headers.TryGetValues("X-Active-Org-ID", out var vals);
            orgHeader = vals is not null ? string.Join(",", vals) : null;
            return Responses.Ok("{\"service\":{\"id\":\"s1\",\"name\":\"svc\",\"status\":\"Pending\",\"database_type\":\"postgresql\"}}");
        });

        var cfg = new FoundryDBConfig
        {
            ApiUrl = "https://api.foundrydb.com",
            Token = "tok",
            OrganizationId = "default-org"
        };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });

        await client.CreateServiceAsync(new CreateServiceRequest
        {
            Name = "svc",
            PlanName = "tier-2",
            OrganizationId = "per-request-org"
        });

        Assert.Equal("per-request-org", orgHeader);
    }

    [Fact]
    public async Task NoOrgId_HeaderIsAbsent()
    {
        bool headerPresent = false;
        var handler = new MockHttpHandler(req =>
        {
            headerPresent = req.Headers.Contains("X-Active-Org-ID");
            return Responses.Ok("{\"services\":[]}");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListServicesAsync();

        Assert.False(headerPresent);
    }

    // ----- Error parsing -----

    [Fact]
    public async Task Http404_ThrowsFoundryDBException_WithStatusCode404()
    {
        var handler = new MockHttpHandler(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.GetServiceAsync("missing-id"));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("not found", ex.Detail);
    }

    [Fact]
    public async Task Http500_ThrowsFoundryDBException_WithStatusCode500()
    {
        var handler = new MockHttpHandler(_ =>
            Responses.Status(HttpStatusCode.InternalServerError, "{\"message\":\"internal server error\"}"));

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.ListServicesAsync());
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("internal server error", ex.Detail);
    }

    [Fact]
    public async Task ErrorBody_WithDetailField_ExtractsDetail()
    {
        var handler = new MockHttpHandler(_ =>
            Responses.Status(HttpStatusCode.UnprocessableEntity, "{\"detail\":\"name too long\",\"title\":\"Validation\"}"));

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.ListServicesAsync());
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("name too long", ex.Detail);
        Assert.Equal("Validation", ex.Title);
    }

    [Fact]
    public async Task ErrorBody_PlainText_UsedAsDetail()
    {
        var handler = new MockHttpHandler(_ =>
            Responses.Status(HttpStatusCode.BadGateway, "upstream timeout"));

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.ListServicesAsync());
        Assert.Equal(502, ex.StatusCode);
        Assert.Equal("upstream timeout", ex.Detail);
    }

    [Fact]
    public async Task ErrorBody_EmptyBody_UsesDefaultTitle()
    {
        var handler = new MockHttpHandler(_ =>
            Responses.Status(HttpStatusCode.ServiceUnavailable, ""));

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.ListServicesAsync());
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("API Error", ex.Title);
    }

    // ----- Top-level shorthand methods delegate to sub-APIs -----

    [Fact]
    public async Task ListServicesAsync_DelegatesToServicesApi()
    {
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"services\":[]}");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        var result = await client.ListServicesAsync();

        Assert.Equal("/managed-services", capturedPath);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetServiceAsync_DelegatesToServicesApi()
    {
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"service\":{\"id\":\"abc\",\"name\":\"pg\",\"status\":\"Running\",\"database_type\":\"postgresql\"}}");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        var svc = await client.GetServiceAsync("abc");

        Assert.Equal("/managed-services/abc", capturedPath);
        Assert.Equal("abc", svc.Id);
    }

    [Fact]
    public async Task DeleteServiceAsync_DelegatesToServicesApi()
    {
        HttpMethod? capturedMethod = null;
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedMethod = req.Method;
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.DeleteServiceAsync("abc");

        Assert.Equal(HttpMethod.Delete, capturedMethod);
        Assert.Equal("/managed-services/abc", capturedPath);
    }

    [Fact]
    public async Task ListOrganizationsAsync_DelegatesToOrganizationsApi()
    {
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("[]");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        var result = await client.ListOrganizationsAsync();

        Assert.Equal("/organizations", capturedPath);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListUsersAsync_DelegatesToUsersApi()
    {
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("[]");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListUsersAsync("svc-1");

        Assert.Equal("/managed-services/svc-1/database-users", capturedPath);
    }

    [Fact]
    public async Task RevealPasswordAsync_DelegatesToUsersApi()
    {
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"password\":\"secret123\"}");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        var pw = await client.RevealPasswordAsync("svc-1", "app_user");

        Assert.Equal("/managed-services/svc-1/database-users/app_user/reveal-password", capturedPath);
        Assert.Equal("secret123", pw);
    }

    [Fact]
    public async Task ListBackupsAsync_DelegatesToBackupsApi()
    {
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("[]");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListBackupsAsync("svc-1");

        Assert.Equal("/managed-services/svc-1/backups", capturedPath);
    }

    [Fact]
    public async Task TriggerBackupAsync_DelegatesToBackupsApi()
    {
        HttpMethod? capturedMethod = null;
        string? capturedPath = null;
        var handler = new MockHttpHandler(req =>
        {
            capturedMethod = req.Method;
            capturedPath = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"backup\":{\"id\":\"b1\",\"service_id\":\"svc-1\",\"status\":\"pending\"}}");
        });

        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        var backup = await client.TriggerBackupAsync("svc-1", new CreateBackupRequest());

        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal("/managed-services/svc-1/backups", capturedPath);
        Assert.Equal("b1", backup.Id);
    }

    // ----- Dispose -----

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutException()
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var client = new FoundryDBClient(cfg);

        client.Dispose();
        client.Dispose(); // Should not throw.
    }

    // ----- Token takes precedence over Basic credentials -----

    [Fact]
    public async Task WhenTokenAndBasicBothProvided_BearerIsUsed()
    {
        AuthenticationHeaderValue? captured = null;
        var handler = new MockHttpHandler(req =>
        {
            captured = req.Headers.Authorization;
            return Responses.Ok("{\"services\":[]}");
        });

        var cfg = new FoundryDBConfig
        {
            ApiUrl = "https://api.foundrydb.com",
            Token = "my-token",
            Username = "user",
            Password = "pass"
        };
        using var client = new FoundryDBClient(cfg, new HttpClient(handler) { BaseAddress = new Uri(cfg.ApiUrl) });
        await client.ListServicesAsync();

        Assert.Equal("Bearer", captured?.Scheme);
    }
}

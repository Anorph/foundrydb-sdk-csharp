using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
using Xunit;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Tests for <see cref="FoundryDB.SDK.Services.ServicesApi"/>.
/// </summary>
public class ServicesApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- ListAsync -----

    [Fact]
    public async Task ListAsync_SendsGetToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"services\":[]}");
        });

        await client.Services.ListAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/managed-services", path);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenServicesArrayIsEmpty()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"services\":[]}"));

        var result = await client.Services.ListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_DeserializesServices_FromServicesProperty()
    {
        var body = JsonSerializer.Serialize(new
        {
            services = new[]
            {
                new { id = "s1", name = "pg-prod", status = "Running", database_type = "postgresql" },
                new { id = "s2", name = "mysql-dev", status = "Pending", database_type = "mysql" }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Services.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("s1", result[0].Id);
        Assert.Equal("pg-prod", result[0].Name);
        Assert.Equal("Running", result[0].Status);
        Assert.Equal("postgresql", result[0].DatabaseType);
        Assert.Equal("s2", result[1].Id);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenServicesPropertyAbsent()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        var result = await client.Services.ListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_Throws_On404()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Services.ListAsync());
        Assert.Equal(404, ex.StatusCode);
    }

    // ----- GetAsync -----

    [Fact]
    public async Task GetAsync_SendsGetToServicePath()
    {
        string? path = null;
        using var client = BuildClient(req =>
        {
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"service\":{\"id\":\"svc-1\",\"name\":\"pg\",\"status\":\"Running\",\"database_type\":\"postgresql\"}}");
        });

        await client.Services.GetAsync("svc-1");

        Assert.Equal("/managed-services/svc-1", path);
    }

    [Fact]
    public async Task GetAsync_DeserializesServiceObject()
    {
        var body = JsonSerializer.Serialize(new
        {
            service = new
            {
                id = "svc-1",
                name = "pg-prod",
                status = "Running",
                database_type = "postgresql",
                version = "17",
                plan_name = "tier-2",
                zone = "se-sto1",
                storage_size_gb = 50,
                node_count = 1,
                organization_id = "org-abc"
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var svc = await client.Services.GetAsync("svc-1");

        Assert.Equal("svc-1", svc.Id);
        Assert.Equal("pg-prod", svc.Name);
        Assert.Equal("Running", svc.Status);
        Assert.Equal("postgresql", svc.DatabaseType);
        Assert.Equal("17", svc.Version);
        Assert.Equal("tier-2", svc.PlanName);
        Assert.Equal("se-sto1", svc.Zone);
        Assert.Equal(50, svc.StorageSizeGb);
        Assert.Equal(1, svc.NodeCount);
        Assert.Equal("org-abc", svc.OrganizationId);
    }

    [Fact]
    public async Task GetAsync_EmptyId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Services.GetAsync("  "));
    }

    [Fact]
    public async Task GetAsync_ThrowsFoundryDBException_On404()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"service not found\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Services.GetAsync("missing"));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("service not found", ex.Detail);
    }

    [Fact]
    public async Task GetAsync_FallsBackToRootDeserialize_WhenServicePropertyAbsent()
    {
        // If response lacks "service" wrapper, the SDK falls back to root-level deserialisation.
        var body = JsonSerializer.Serialize(new
        {
            id = "svc-x",
            name = "direct",
            status = "Running",
            database_type = "valkey"
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var svc = await client.Services.GetAsync("svc-x");

        Assert.Equal("svc-x", svc.Id);
        Assert.Equal("direct", svc.Name);
    }

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"service\":{\"id\":\"new-svc\",\"name\":\"pg\",\"status\":\"Pending\",\"database_type\":\"postgresql\"}}");
        });

        await client.Services.CreateAsync(new CreateServiceRequest
        {
            Name = "pg",
            PlanName = "tier-2"
        });

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/managed-services", path);
    }

    [Fact]
    public async Task CreateAsync_DeserializesCreatedService()
    {
        var body = JsonSerializer.Serialize(new
        {
            service = new
            {
                id = "new-svc",
                name = "pg",
                status = "Pending",
                database_type = "postgresql",
                version = "17"
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var svc = await client.Services.CreateAsync(new CreateServiceRequest
        {
            Name = "pg",
            PlanName = "tier-2",
            DatabaseType = DatabaseType.PostgreSQL,
            Version = "17"
        });

        Assert.Equal("new-svc", svc.Id);
        Assert.Equal("pg", svc.Name);
        Assert.Equal("Pending", svc.Status);
    }

    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Services.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Services.CreateAsync(new CreateServiceRequest
        {
            Name = "",
            PlanName = "tier-2"
        }));
    }

    [Fact]
    public async Task CreateAsync_EmptyPlanName_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Services.CreateAsync(new CreateServiceRequest
        {
            Name = "svc",
            PlanName = "  "
        }));
    }

    [Fact]
    public async Task CreateAsync_WithOrgId_SendsOrgHeader()
    {
        string? orgHeader = null;
        using var client = BuildClient(req =>
        {
            req.Headers.TryGetValues("X-Active-Org-ID", out var vals);
            orgHeader = vals is not null ? string.Join(",", vals) : null;
            return Responses.Ok("{\"service\":{\"id\":\"s\",\"name\":\"n\",\"status\":\"Pending\",\"database_type\":\"postgresql\"}}");
        });

        await client.Services.CreateAsync(new CreateServiceRequest
        {
            Name = "svc",
            PlanName = "tier-2",
            OrganizationId = "my-org"
        });

        Assert.Equal("my-org", orgHeader);
    }

    [Fact]
    public async Task CreateAsync_ThrowsFoundryDBException_OnError()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.UnprocessableEntity, "{\"error\":\"invalid plan\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Services.CreateAsync(new CreateServiceRequest
        {
            Name = "svc",
            PlanName = "bad-plan"
        }));
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("invalid plan", ex.Detail);
    }

    // ----- DeleteAsync -----

    [Fact]
    public async Task DeleteAsync_SendsDeleteToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("");
        });

        await client.Services.DeleteAsync("svc-1");

        Assert.Equal(HttpMethod.Delete, method);
        Assert.Equal("/managed-services/svc-1", path);
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok(""));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Services.DeleteAsync(""));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsFoundryDBException_On404()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"not found\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Services.DeleteAsync("bad-id"));
        Assert.Equal(404, ex.StatusCode);
    }

    // ----- WaitForRunningAsync -----

    [Fact]
    public async Task WaitForRunningAsync_EmptyId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Services.WaitForRunningAsync("", timeout: TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task WaitForRunningAsync_ServiceAlreadyRunning_ReturnsImmediately()
    {
        var body = JsonSerializer.Serialize(new
        {
            service = new { id = "s1", name = "pg", status = "Running", database_type = "postgresql" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var svc = await client.Services.WaitForRunningAsync("s1", timeout: TimeSpan.FromSeconds(5));

        Assert.Equal("Running", svc.Status);
    }

    [Fact]
    public async Task WaitForRunningAsync_FailedState_ThrowsFoundryDBException()
    {
        var body = JsonSerializer.Serialize(new
        {
            service = new { id = "s1", name = "pg", status = "Failed", database_type = "postgresql" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Services.WaitForRunningAsync("s1", timeout: TimeSpan.FromSeconds(5)));

        Assert.Equal(0, ex.StatusCode);
        Assert.Contains("Failed", ex.Title);
    }

    [Fact]
    public async Task WaitForRunningAsync_Timeout_ThrowsTimeoutException()
    {
        // Always return a non-terminal state so the poller keeps spinning.
        var body = JsonSerializer.Serialize(new
        {
            service = new { id = "s1", name = "pg", status = "SetupServer", database_type = "postgresql" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        // Use a very short timeout so the test finishes quickly.
        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.Services.WaitForRunningAsync("s1", timeout: TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task WaitForRunningAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Return non-Running so polling continues.
        var body = JsonSerializer.Serialize(new
        {
            service = new { id = "s1", name = "pg", status = "SetupDNS", database_type = "postgresql" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.Services.WaitForRunningAsync("s1", timeout: TimeSpan.FromMinutes(1), ct: cts.Token));
    }

    [Fact]
    public async Task WaitForRunningAsync_EventuallyRunning_ReturnsService()
    {
        int callCount = 0;
        using var client = BuildClient(_ =>
        {
            callCount++;
            // Return Pending for first two calls, Running on the third.
            var status = callCount < 3 ? "Pending" : "Running";
            var body = JsonSerializer.Serialize(new
            {
                service = new { id = "s1", name = "pg", status, database_type = "postgresql" }
            });
            return Responses.Ok(body);
        });

        // Use a generous timeout so the test is not flaky, but a tiny poll interval
        // is emulated by calling with a short timeout window relative to the delay.
        // Since WaitForRunningAsync calls Task.Delay(10s) between polls, we override
        // at the test level by using a very long timeout and cancellation after success.
        var svc = await client.Services.WaitForRunningAsync("s1", timeout: TimeSpan.FromMinutes(2));

        Assert.Equal("Running", svc.Status);
        Assert.True(callCount >= 3);
    }
}

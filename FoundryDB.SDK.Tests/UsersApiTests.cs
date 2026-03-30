using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FoundryDB.SDK;
using Xunit;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Tests for <see cref="FoundryDB.SDK.Users.UsersApi"/>.
/// </summary>
public class UsersApiTests
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
            return Responses.Ok("[]");
        });

        await client.Users.ListAsync("svc-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/managed-services/svc-1/database-users", path);
    }

    [Fact]
    public async Task ListAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Users.ListAsync(""));
    }

    [Fact]
    public async Task ListAsync_WhitespaceServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Users.ListAsync("   "));
    }

    [Fact]
    public async Task ListAsync_DeserializesUsers_FromRootArray()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { username = "app_user", is_admin = true, databases = new[] { "defaultdb" } },
            new { username = "readonly", is_admin = false, databases = new[] { "defaultdb", "analytics" } }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Users.ListAsync("svc-1");

        Assert.Equal(2, result.Count);
        Assert.Equal("app_user", result[0].Username);
        Assert.Equal(true, result[0].IsAdmin);
        Assert.Contains("defaultdb", result[0].Databases!);
        Assert.Equal("readonly", result[1].Username);
        Assert.Equal(false, result[1].IsAdmin);
        Assert.Equal(2, result[1].Databases!.Count);
    }

    [Fact]
    public async Task ListAsync_DeserializesUsers_FromUsersProperty()
    {
        var body = JsonSerializer.Serialize(new
        {
            users = new[]
            {
                new { username = "wrapped_user", is_admin = false }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Users.ListAsync("svc-1");

        Assert.Single(result);
        Assert.Equal("wrapped_user", result[0].Username);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNeitherArrayNorProperty()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        var result = await client.Users.ListAsync("svc-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_ThrowsFoundryDBException_On404()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"service not found\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Users.ListAsync("bad-id"));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ListAsync_ThrowsFoundryDBException_On500()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.InternalServerError, "{\"message\":\"error\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Users.ListAsync("svc-1"));
        Assert.Equal(500, ex.StatusCode);
    }

    // ----- RevealPasswordAsync -----

    [Fact]
    public async Task RevealPasswordAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"password\":\"secret\"}");
        });

        await client.Users.RevealPasswordAsync("svc-1", "app_user");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/managed-services/svc-1/database-users/app_user/reveal-password", path);
    }

    [Fact]
    public async Task RevealPasswordAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"password\":\"x\"}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Users.RevealPasswordAsync("", "user"));
    }

    [Fact]
    public async Task RevealPasswordAsync_EmptyUsername_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"password\":\"x\"}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Users.RevealPasswordAsync("svc-1", ""));
    }

    [Fact]
    public async Task RevealPasswordAsync_ExtractsPasswordFromPasswordField()
    {
        using var client = BuildClient(_ => Responses.Ok("{\"password\":\"hunter2\"}"));

        var pw = await client.Users.RevealPasswordAsync("svc-1", "app_user");

        Assert.Equal("hunter2", pw);
    }

    [Fact]
    public async Task RevealPasswordAsync_ExtractsPassword_FromRootString()
    {
        // Some endpoints return the password as a plain JSON string at root level.
        using var client = BuildClient(_ => Responses.Ok("\"p4ssw0rd\""));

        var pw = await client.Users.RevealPasswordAsync("svc-1", "app_user");

        Assert.Equal("p4ssw0rd", pw);
    }

    [Fact]
    public async Task RevealPasswordAsync_NoPasswordField_ThrowsFoundryDBException()
    {
        // Response has neither "password" field nor root string.
        using var client = BuildClient(_ => Responses.Ok("{\"something\":\"else\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Users.RevealPasswordAsync("svc-1", "user"));
        Assert.Equal(200, ex.StatusCode);
        Assert.Contains("password", ex.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RevealPasswordAsync_ThrowsFoundryDBException_On404()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"user not found\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Users.RevealPasswordAsync("svc-1", "missing"));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task RevealPasswordAsync_ThrowsFoundryDBException_On500()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.InternalServerError, "{\"error\":\"vault error\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Users.RevealPasswordAsync("svc-1", "app_user"));
        Assert.Equal(500, ex.StatusCode);
    }

    // ----- CreatedAt deserialization -----

    [Fact]
    public async Task ListAsync_DeserializesCreatedAt()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { username = "u1", created_at = "2026-01-01T00:00:00Z" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Users.ListAsync("svc-1");

        Assert.Single(result);
        Assert.Equal(2026, result[0].CreatedAt!.Value.Year);
    }
}

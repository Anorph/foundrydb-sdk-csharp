using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FoundryDB.SDK;
using FoundryDB.SDK.Models;
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
            new { username = "app_user", roles = new[] { "readwrite" } },
            new { username = "readonly", roles = new[] { "readonly" } }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Users.ListAsync("svc-1");

        Assert.Equal(2, result.Count);
        Assert.Equal("app_user", result[0].Username);
        Assert.Contains("readwrite", result[0].Roles!);
        Assert.Equal("readonly", result[1].Username);
        Assert.Contains("readonly", result[1].Roles!);
    }

    [Fact]
    public async Task ListAsync_DeserializesUsers_FromUsersProperty()
    {
        var body = JsonSerializer.Serialize(new
        {
            users = new[]
            {
                new { username = "wrapped_user", roles = new[] { "admin" } }
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
            return Responses.Ok("{\"username\":\"app_user\",\"password\":\"secret\",\"host\":\"db.example.com\",\"port\":5432,\"database\":\"defaultdb\",\"connection_string\":\"postgresql://app_user:secret@db.example.com:5432/defaultdb\"}");
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
    public async Task RevealPasswordAsync_ReturnsFullCredentials()
    {
        var body = JsonSerializer.Serialize(new
        {
            username = "app_user",
            password = "hunter2",
            host = "db.example.com",
            port = 5432L,
            database = "defaultdb",
            connection_string = "postgresql://app_user:hunter2@db.example.com:5432/defaultdb"
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var creds = await client.Users.RevealPasswordAsync("svc-1", "app_user");

        Assert.Equal("app_user", creds.Username);
        Assert.Equal("hunter2", creds.Password);
        Assert.Equal("db.example.com", creds.Host);
        Assert.Equal(5432L, creds.Port);
        Assert.Equal("defaultdb", creds.Database);
        Assert.NotNull(creds.ConnectionString);
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

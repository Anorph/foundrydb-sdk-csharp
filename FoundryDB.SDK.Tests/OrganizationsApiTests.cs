using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FoundryDB.SDK;
using Xunit;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Tests for <see cref="FoundryDB.SDK.Organizations.OrganizationsApi"/>.
/// </summary>
public class OrganizationsApiTests
{
    private static FoundryDBClient BuildClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var cfg = new FoundryDBConfig { ApiUrl = "https://api.foundrydb.com", Token = "tok" };
        var http = new HttpClient(new MockHttpHandler(handler)) { BaseAddress = new Uri(cfg.ApiUrl) };
        return new FoundryDBClient(cfg, http);
    }

    // ----- ListAsync - GET path -----

    [Fact]
    public async Task ListAsync_SendsGetToOrganizationsPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("[]");
        });

        await client.Organizations.ListAsync();

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/organizations", path);
    }

    // ----- ListAsync - root array response -----

    [Fact]
    public async Task ListAsync_DeserializesOrgs_FromRootArray()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { id = "o1", name = "Acme", slug = "acme", is_personal = false, role = "admin" },
            new { id = "o2", name = "Personal", slug = "personal", is_personal = true, role = "owner" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Organizations.ListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("o1", result[0].Id);
        Assert.Equal("Acme", result[0].Name);
        Assert.Equal("acme", result[0].Slug);
        Assert.Equal(false, result[0].IsPersonal);
        Assert.Equal("admin", result[0].Role);
        Assert.Equal("o2", result[1].Id);
        Assert.Equal(true, result[1].IsPersonal);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_FromEmptyRootArray()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));

        var result = await client.Organizations.ListAsync();

        Assert.Empty(result);
    }

    // ----- ListAsync - wrapped response -----

    [Fact]
    public async Task ListAsync_DeserializesOrgs_FromOrganizationsProperty()
    {
        var body = JsonSerializer.Serialize(new
        {
            organizations = new[]
            {
                new { id = "o1", name = "Wrapped Org", slug = "wrapped", is_personal = false }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Organizations.ListAsync();

        Assert.Single(result);
        Assert.Equal("o1", result[0].Id);
        Assert.Equal("Wrapped Org", result[0].Name);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenNoArrayAndNoProperty()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        var result = await client.Organizations.ListAsync();

        Assert.Empty(result);
    }

    // ----- Auth header -----

    [Fact]
    public async Task ListAsync_DoesNotSendOrgHeader_WhenConfigHasNoOrgId()
    {
        bool headerPresent = false;
        using var client = BuildClient(req =>
        {
            headerPresent = req.Headers.Contains("X-Active-Org-ID");
            return Responses.Ok("[]");
        });

        await client.Organizations.ListAsync();

        Assert.False(headerPresent);
    }

    [Fact]
    public async Task ListAsync_SendsOrgHeader_WhenConfigHasOrgId()
    {
        string? orgHeader = null;
        var cfg = new FoundryDBConfig
        {
            ApiUrl = "https://api.foundrydb.com",
            Token = "tok",
            OrganizationId = "config-org"
        };
        var http = new HttpClient(new MockHttpHandler(req =>
        {
            req.Headers.TryGetValues("X-Active-Org-ID", out var vals);
            orgHeader = vals is not null ? string.Join(",", vals) : null;
            return Responses.Ok("[]");
        })) { BaseAddress = new Uri(cfg.ApiUrl) };
        using var client = new FoundryDBClient(cfg, http);

        await client.Organizations.ListAsync();

        Assert.Equal("config-org", orgHeader);
    }

    // ----- Error handling -----

    [Fact]
    public async Task ListAsync_ThrowsFoundryDBException_On401()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Organizations.ListAsync());
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task ListAsync_ThrowsFoundryDBException_On500()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.InternalServerError, "{\"message\":\"server error\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Organizations.ListAsync());
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("server error", ex.Detail);
    }

    // ----- CreatedAt deserialization -----

    [Fact]
    public async Task ListAsync_DeserializesCreatedAt()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { id = "o1", name = "Org", created_at = "2025-01-15T10:00:00Z" }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Organizations.ListAsync();

        Assert.Single(result);
        Assert.NotNull(result[0].CreatedAt);
        Assert.Equal(2025, result[0].CreatedAt!.Value.Year);
    }
}

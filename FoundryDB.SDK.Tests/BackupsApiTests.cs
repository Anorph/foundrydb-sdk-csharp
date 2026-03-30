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
/// Tests for <see cref="FoundryDB.SDK.Backups.BackupsApi"/>.
/// </summary>
public class BackupsApiTests
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

        await client.Backups.ListAsync("svc-1");

        Assert.Equal(HttpMethod.Get, method);
        Assert.Equal("/managed-services/svc-1/backups", path);
    }

    [Fact]
    public async Task ListAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Backups.ListAsync(""));
    }

    [Fact]
    public async Task ListAsync_WhitespaceServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.Backups.ListAsync("  "));
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenResponseIsEmptyArray()
    {
        using var client = BuildClient(_ => Responses.Ok("[]"));

        var result = await client.Backups.ListAsync("svc-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_DeserializesBackups_FromRootArray()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = "b1",
                service_id = "svc-1",
                status = "completed",
                backup_type = "full",
                size_bytes = 1073741824L,
                size_label = "1.0 GB",
                created_at = "2026-01-01T00:00:00Z",
                completed_at = "2026-01-01T00:10:00Z"
            },
            new
            {
                id = "b2",
                service_id = "svc-1",
                status = "failed",
                backup_type = "incremental",
                error_message = "disk full",
                created_at = "2026-01-02T00:00:00Z"
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Backups.ListAsync("svc-1");

        Assert.Equal(2, result.Count);

        Assert.Equal("b1", result[0].Id);
        Assert.Equal("svc-1", result[0].ServiceId);
        Assert.Equal("completed", result[0].Status);
        Assert.Equal("full", result[0].BackupType);
        Assert.Equal(1073741824L, result[0].SizeBytes);
        Assert.Equal("1.0 GB", result[0].SizeLabel);
        Assert.Equal(2026, result[0].CreatedAt!.Value.Year);
        Assert.NotNull(result[0].CompletedAt);

        Assert.Equal("b2", result[1].Id);
        Assert.Equal("failed", result[1].Status);
        Assert.Equal("disk full", result[1].ErrorMessage);
        Assert.Null(result[1].CompletedAt);
    }

    [Fact]
    public async Task ListAsync_DeserializesBackups_FromBackupsProperty()
    {
        var body = JsonSerializer.Serialize(new
        {
            backups = new[]
            {
                new { id = "b1", service_id = "svc-1", status = "completed", backup_type = "full" }
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var result = await client.Backups.ListAsync("svc-1");

        Assert.Single(result);
        Assert.Equal("b1", result[0].Id);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNeitherArrayNorProperty()
    {
        using var client = BuildClient(_ => Responses.Ok("{}"));

        var result = await client.Backups.ListAsync("svc-1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_ThrowsFoundryDBException_On404()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.NotFound, "{\"error\":\"service not found\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Backups.ListAsync("bad-id"));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("service not found", ex.Detail);
    }

    [Fact]
    public async Task ListAsync_ThrowsFoundryDBException_On500()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.InternalServerError, "{\"message\":\"backend error\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() => client.Backups.ListAsync("svc-1"));
        Assert.Equal(500, ex.StatusCode);
    }

    // ----- TriggerAsync -----

    [Fact]
    public async Task TriggerAsync_SendsPostToCorrectPath()
    {
        HttpMethod? method = null;
        string? path = null;
        using var client = BuildClient(req =>
        {
            method = req.Method;
            path = req.RequestUri?.PathAndQuery;
            return Responses.Ok("{\"backup\":{\"id\":\"b1\",\"service_id\":\"svc-1\",\"status\":\"pending\"}}");
        });

        await client.Backups.TriggerAsync("svc-1", new CreateBackupRequest());

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/managed-services/svc-1/backups", path);
    }

    [Fact]
    public async Task TriggerAsync_EmptyServiceId_ThrowsArgumentException()
    {
        using var client = BuildClient(_ =>
            Responses.Ok("{\"backup\":{\"id\":\"b1\",\"service_id\":\"s\",\"status\":\"pending\"}}"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.Backups.TriggerAsync("", new CreateBackupRequest()));
    }

    [Fact]
    public async Task TriggerAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var client = BuildClient(_ =>
            Responses.Ok("{\"backup\":{\"id\":\"b1\",\"service_id\":\"s\",\"status\":\"pending\"}}"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.Backups.TriggerAsync("svc-1", null!));
    }

    [Fact]
    public async Task TriggerAsync_DeserializesBackup_FromBackupProperty()
    {
        var body = JsonSerializer.Serialize(new
        {
            backup = new
            {
                id = "b1",
                service_id = "svc-1",
                status = "pending",
                backup_type = "full",
                created_at = "2026-03-01T12:00:00Z"
            }
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var backup = await client.Backups.TriggerAsync("svc-1", new CreateBackupRequest
        {
            BackupType = BackupType.Full,
            Label = "pre-migration"
        });

        Assert.Equal("b1", backup.Id);
        Assert.Equal("svc-1", backup.ServiceId);
        Assert.Equal("pending", backup.Status);
        Assert.Equal("full", backup.BackupType);
        Assert.Equal(2026, backup.CreatedAt!.Value.Year);
    }

    [Fact]
    public async Task TriggerAsync_DeserializesBackup_FromRootFallback()
    {
        // When no "backup" wrapper, the root object is treated as the Backup.
        var body = JsonSerializer.Serialize(new
        {
            id = "b2",
            service_id = "svc-1",
            status = "pending",
            backup_type = "incremental"
        });
        using var client = BuildClient(_ => Responses.Ok(body));

        var backup = await client.Backups.TriggerAsync("svc-1", new CreateBackupRequest
        {
            BackupType = BackupType.Incremental
        });

        Assert.Equal("b2", backup.Id);
        Assert.Equal("pending", backup.Status);
        Assert.Equal("incremental", backup.BackupType);
    }

    [Fact]
    public async Task TriggerAsync_ThrowsFoundryDBException_WhenResponseCannotBeDeserialized()
    {
        // Neither "backup" wrapper nor a valid Backup root - triggers ThrowIfNull.
        using var client = BuildClient(_ => Responses.Ok("null"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Backups.TriggerAsync("svc-1", new CreateBackupRequest()));
        Assert.Equal(200, ex.StatusCode);
        Assert.Contains("backup", ex.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerAsync_ThrowsFoundryDBException_On422()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.UnprocessableEntity, "{\"error\":\"invalid backup type\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Backups.TriggerAsync("svc-1", new CreateBackupRequest { BackupType = BackupType.Incremental }));
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("invalid backup type", ex.Detail);
    }

    [Fact]
    public async Task TriggerAsync_ThrowsFoundryDBException_On500()
    {
        using var client = BuildClient(_ =>
            Responses.Status(HttpStatusCode.InternalServerError, "{\"message\":\"s3 unavailable\"}"));

        var ex = await Assert.ThrowsAsync<FoundryDBException>(() =>
            client.Backups.TriggerAsync("svc-1", new CreateBackupRequest()));
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("s3 unavailable", ex.Detail);
    }

    // ----- Serialisation of CreateBackupRequest body -----

    [Fact]
    public async Task TriggerAsync_SendsCorrectJsonBody_WithBackupType()
    {
        string? requestBody = null;
        using var client = BuildClient(async req =>
        {
            requestBody = req.Content is not null
                ? await req.Content.ReadAsStringAsync()
                : null;
            return Responses.Ok("{\"backup\":{\"id\":\"b1\",\"service_id\":\"svc-1\",\"status\":\"pending\"}}");
        });

        await client.Backups.TriggerAsync("svc-1", new CreateBackupRequest
        {
            BackupType = BackupType.Full,
            Label = "nightly"
        });

        Assert.NotNull(requestBody);
        using var doc = System.Text.Json.JsonDocument.Parse(requestBody!);
        Assert.True(doc.RootElement.TryGetProperty("backup_type", out var btEl));
        Assert.Equal("Full", btEl.GetString());
        Assert.True(doc.RootElement.TryGetProperty("label", out var labelEl));
        Assert.Equal("nightly", labelEl.GetString());
    }
}

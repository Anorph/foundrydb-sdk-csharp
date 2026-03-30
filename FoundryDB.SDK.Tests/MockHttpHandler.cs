using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoundryDB.SDK.Tests;

/// <summary>
/// Intercepts outgoing HTTP requests and delegates them to an in-memory handler function.
/// Allows tests to assert on request properties and return pre-built responses.
/// </summary>
public class MockHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_handler(request));
}

/// <summary>
/// Factory helpers for building commonly needed mock responses.
/// </summary>
public static class Responses
{
    public static HttpResponseMessage Ok(string json)
        => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage Status(HttpStatusCode code, string body = "")
        => new HttpResponseMessage(code)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
}

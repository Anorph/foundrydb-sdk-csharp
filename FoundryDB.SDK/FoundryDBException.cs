namespace FoundryDB.SDK;

/// <summary>
/// Thrown when the FoundryDB API returns a non-success HTTP status code.
/// </summary>
public class FoundryDBException : Exception
{
    /// <summary>HTTP status code returned by the API.</summary>
    public int StatusCode { get; }

    /// <summary>Short title extracted from the error response.</summary>
    public string Title { get; }

    /// <summary>Detailed error message from the API.</summary>
    public string Detail { get; }

    /// <summary>
    /// Creates a new <see cref="FoundryDBException"/>.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="title">Short error title.</param>
    /// <param name="detail">Detailed description of the error.</param>
    public FoundryDBException(int statusCode, string title, string detail)
        : base($"[HTTP {statusCode}] {title}: {detail}")
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
    }
}

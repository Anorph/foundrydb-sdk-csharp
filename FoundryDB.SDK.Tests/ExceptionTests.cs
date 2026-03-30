using System;
using FoundryDB.SDK;
using Xunit;

namespace FoundryDB.SDK.Tests;

public class ExceptionTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var ex = new FoundryDBException(404, "Not Found", "Service does not exist.");

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Not Found", ex.Title);
        Assert.Equal("Service does not exist.", ex.Detail);
    }

    [Fact]
    public void Message_ContainsStatusCodeTitleAndDetail()
    {
        var ex = new FoundryDBException(422, "Validation Error", "Name is required.");

        Assert.Contains("422", ex.Message);
        Assert.Contains("Validation Error", ex.Message);
        Assert.Contains("Name is required.", ex.Message);
    }

    [Fact]
    public void IsException_DerivedFromSystemException()
    {
        var ex = new FoundryDBException(500, "Internal Error", "Something went wrong.");

        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void StatusCode_Zero_IsAllowed()
    {
        // Status 0 is used internally for terminal service states (e.g. Failed).
        var ex = new FoundryDBException(0, "Service Failed", "Service reached failed state.");

        Assert.Equal(0, ex.StatusCode);
        Assert.Equal("Service Failed", ex.Title);
        Assert.Equal("Service reached failed state.", ex.Detail);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_DoesNotThrow()
    {
        var ex = new FoundryDBException(200, string.Empty, string.Empty);

        Assert.Equal(200, ex.StatusCode);
        Assert.Equal(string.Empty, ex.Title);
        Assert.Equal(string.Empty, ex.Detail);
    }

    [Fact]
    public void CanBeThrown_AndCaught()
    {
        var thrownEx = Assert.Throws<FoundryDBException>(() =>
            throw new FoundryDBException(403, "Forbidden", "Access denied."));

        Assert.Equal(403, thrownEx.StatusCode);
        Assert.Equal("Forbidden", thrownEx.Title);
        Assert.Equal("Access denied.", thrownEx.Detail);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.HttpResults;

public partial class CreatedAtRouteOfTResultTests
{
    [Fact]
    public void CreatedAtRouteResult_ProblemDetails_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var routeValues = new RouteValueDictionary(new Dictionary<string, string>()
        {
            { "test", "case" },
            { "sample", "route" }
        });
        var obj = new HttpValidationProblemDetails();
        var result = new CreatedAtRoute<HttpValidationProblemDetails>(routeValues, obj);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(StatusCodes.Status201Created, obj.Status);
        Assert.Equal(obj, result.Value);
    }
    public static IEnumerable<object[]> CreatedAtRouteData
    {
        get
        {
            yield return new object[] { null };
            yield return
                new object[] {
                        new Dictionary<string, string>() { { "hello", "world" } }
                };
            yield return
                new object[] {
                        new RouteValueDictionary(new Dictionary<string, string>() {
                            { "test", "case" },
                            { "sample", "route" }
                        })
                };
        }
    }

    [Theory]
    [MemberData(nameof(CreatedAtRouteData))]
    public async Task CreatedAtRouteResult_ReturnsStatusCode_SetsLocationHeader(object values)
    {
        // Arrange
        var expectedUrl = "testAction";
        var httpContext = GetHttpContext(expectedUrl);

        // Act
        var result = new CreatedAtRoute<object>(routeName: null, routeValues: values, value: null);
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task CreatedAtRouteResult_ThrowsOnNullUrl()
    {
        // Arrange
        var httpContext = GetHttpContext(expectedUrl: null);

        var result = new CreatedAtRoute<object>(
            routeName: null,
            routeValues: new Dictionary<string, object>(),
            value: null);

        // Act & Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
            async () => await result.ExecuteAsync(httpContext),
        "No route matches the supplied values.");
    }

    [Fact]
    public void PopulateMetadata_AddsResponseTypeMetadata()
    {
        // Arrange
        CreatedAtRoute<Todo> MyApi() { throw new NotImplementedException(); }
        var metadata = new List<object>();
        var context = new EndpointMetadataContext(((Delegate)MyApi).GetMethodInfo(), metadata, EmptyServiceProvider.Instance);

        // Act
        PopulateMetadata<CreatedAtRoute<Todo>>(context);

        // Assert
        var producesResponseTypeMetadata = context.EndpointMetadata.OfType<ProducesResponseTypeMetadata>().Last();
        Assert.Equal(StatusCodes.Status201Created, producesResponseTypeMetadata.StatusCode);
        Assert.Equal(typeof(Todo), producesResponseTypeMetadata.Type);
        Assert.Single(producesResponseTypeMetadata.ContentTypes, "application/json");
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new CreatedAtRoute<object>(null, null);
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void PopulateMetadata_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("context", () => PopulateMetadata<CreatedAtRoute<object>>(null));
    }

    private static void PopulateMetadata<TResult>(EndpointMetadataContext context)
        where TResult : IEndpointMetadataProvider => TResult.PopulateMetadata(context);

    private record Todo(int Id, string Title);

    private static HttpContext GetHttpContext(string expectedUrl)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices(expectedUrl);
        return httpContext;
    }

    private static IServiceProvider CreateServices(string expectedUrl)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<LinkGenerator>(new TestLinkGenerator
        {
            Url = expectedUrl
        });

        return services.BuildServiceProvider();
    }
}

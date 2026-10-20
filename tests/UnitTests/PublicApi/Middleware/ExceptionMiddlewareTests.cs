using System.Text.Json;
using BlazorShared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.PublicApi.Middleware;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.PublicApi.Middleware;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokesAndAwaitsNextDelegateWithoutChangingSuccessfulResponse()
    {
        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocationCount = 0;
        var middleware = new ExceptionMiddleware(async nextContext =>
        {
            invocationCount++;
            Assert.Same(context, nextContext);
            await completion.Task;
            nextContext.Response.StatusCode = StatusCodes.Status202Accepted;
            nextContext.Response.ContentType = "text/plain";
            nextContext.Response.Headers["X-Downstream"] = "preserved";
            await nextContext.Response.WriteAsync("Success");
        });

        var invocation = middleware.InvokeAsync(context);

        Assert.Equal(1, invocationCount);
        Assert.False(invocation.IsCompleted);
        completion.SetResult();
        await invocation;

        Assert.Equal(1, invocationCount);
        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.Equal("preserved", context.Response.Headers["X-Downstream"].ToString());
        body.Position = 0;
        using var reader = new StreamReader(body);
        Assert.Equal("Success", await reader.ReadToEndAsync());
    }

    [Theory]
    [InlineData(true, false, StatusCodes.Status409Conflict)]
    [InlineData(true, true, StatusCodes.Status409Conflict)]
    [InlineData(false, false, StatusCodes.Status500InternalServerError)]
    [InlineData(false, true, StatusCodes.Status500InternalServerError)]
    public async Task WritesJsonErrorForSynchronousAndAsynchronousExceptions(
        bool duplicate, bool asynchronous, int expectedStatusCode)
    {
        const string message = "Failure with \"quotes\", \\slashes,\na newline and café";
        Exception exception = duplicate
            ? new DuplicateException(message)
            : new InvalidOperationException(message);
        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        context.Response.StatusCode = StatusCodes.Status202Accepted;
        context.Response.ContentType = "text/plain";
        var invocationCount = 0;
        var middleware = new ExceptionMiddleware(nextContext =>
        {
            invocationCount++;
            Assert.Same(context, nextContext);
            if (asynchronous)
            {
                return ThrowAsync();
            }

            throw exception;
        });

        async Task ThrowAsync()
        {
            await Task.Yield();
            throw exception;
        }

        await middleware.InvokeAsync(context);

        Assert.Equal(1, invocationCount);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ErrorDetails>(body);
        Assert.NotNull(error);
        Assert.Equal(expectedStatusCode, error.StatusCode);
        Assert.Equal(message, error.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PropagatesFailureWhenWritingAnErrorResponse(bool duplicate)
    {
        Exception exception = duplicate
            ? new DuplicateException("Duplicate")
            : new InvalidOperationException("Failure");
        var writeException = new IOException("Cannot write the response");
        using var body = Substitute.For<Stream>();
        body.CanWrite.Returns(true);
        body.WriteAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException(writeException));
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var middleware = new ExceptionMiddleware(_ => Task.FromException(exception));

        var actual = await Assert.ThrowsAsync<IOException>(() => middleware.InvokeAsync(context));

        Assert.Same(writeException, actual);
    }
}

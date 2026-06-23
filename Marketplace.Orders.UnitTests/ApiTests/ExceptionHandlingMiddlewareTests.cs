using System.Net;
using System.Text.Json;
using FluentAssertions;
using Marketplace.Orders.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Marketplace.Orders.UnitTests.ApiTests;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware_WhenNoExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK); // 200 по умолчанию
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnNotFound_WhenKeyNotFoundExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // Нам нужно подменить поток ответа, чтобы прочитать, что middleware туда запишет
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new KeyNotFoundException("Заказ не найден");

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        context.Response.ContentType.Should().Be("application/json");

        // Проверяем содержимое ответа
        responseStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonResult = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
        jsonResult.Should().ContainKey("error");
        jsonResult!["error"].Should().Be("Заказ не найден");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnInternalServerError_WhenGenericExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        RequestDelegate next = (ctx) => throw new InvalidOperationException("Критический сбой БД");

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    
        responseStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseStream);
        var responseBody = await reader.ReadToEndAsync();

        var jsonResult = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
        jsonResult.Should().ContainKey("error");
        jsonResult!["error"].Should().Contain("Критический сбой БД");
    
        // Проверяем, что ошибка была залогирована
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Произошла непредвиденная ошибка")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
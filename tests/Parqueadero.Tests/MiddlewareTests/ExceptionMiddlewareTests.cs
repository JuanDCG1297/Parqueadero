using System.Net;
using System.Text.Json;
using Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Web.Api.Middleware;

namespace Parqueadero.Tests.MiddlewareTests;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock = new();
    private readonly DefaultHttpContext _context = new();
    private readonly ExceptionMiddleware _sut;

    public ExceptionMiddlewareTests()
    {
        // Arrange a minimal request path
        _context.Request.Path = "/api/test";
        _context.TraceIdentifier = "test-trace-id";

        // Always pass through to next unless exception
        _sut = new ExceptionMiddleware(
            _ => throw new InvalidOperationException("Should not reach here for exception tests"),
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ShouldReturn404_WhenNotFoundExceptionThrown()
    {
        // Arrange
        var middleware = new ExceptionMiddleware(
            _ => throw new NotFoundException("Vehicle not found."),
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        _context.Response.ContentType.Should().Contain("application/problem+json");
    }

    [Fact]
    public async Task ShouldReturn409_WhenConflictExceptionThrown()
    {
        // Arrange
        var middleware = new ExceptionMiddleware(
            _ => throw new ConflictException("Already parked."),
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task ShouldReturn422_WhenValidationExceptionThrown()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Plate", "Plate is required.")
        };
        var middleware = new ExceptionMiddleware(
            _ => throw new ValidationException(failures),
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task ShouldReturn500_WhenGenericExceptionThrown()
    {
        // Arrange
        var middleware = new ExceptionMiddleware(
            _ => throw new InvalidOperationException("Unexpected error."),
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ShouldNotCallResponse_WhenNoExceptionThrown()
    {
        // Arrange
        var middleware = new ExceptionMiddleware(
            _ => Task.CompletedTask, // No exception
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        _context.Response.StatusCode.Should().Be(StatusCodes.Status200OK); // Default
    }

    [Fact]
    public async Task ShouldIncludeErrorDetailsInBody_WhenValidationFails()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Plate", "Plate is required."),
            new("Plate", "Plate must be alphanumeric.")
        };
        var middleware = new ExceptionMiddleware(
            _ => throw new ValidationException(failures),
            _loggerMock.Object
        );
        _context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(_context);
        _context.Response.Body.Seek(0, SeekOrigin.Begin);

        // Assert
        var body = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        body.Should().NotBeNullOrEmpty();

        var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetInt32().Should().Be(422);
        document.RootElement.GetProperty("title").GetString().Should().Be("Validation Failed");
    }
}

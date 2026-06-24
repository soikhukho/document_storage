using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Commands;

public class InitUploadCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly StorageOptions _options = new();
    private readonly InitUploadCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public InitUploadCommandHandlerTests()
    {
        _handler = new InitUploadCommandHandler(_storage, _options);
    }

    [Fact]
    public async Task HandleAsync_ValidFile_ReturnsSuccessWithUploadUrl()
    {
        var instruction = new UploadInstruction(
            "https://s3.amazonaws.com/bucket/key",
            new Dictionary<string, string> { ["Content-Type"] = "application/pdf" },
            DateTime.UtcNow.AddMinutes(5));

        _storage.InitUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(instruction);

        var command = new InitUploadCommand(ProjectId, "doc.pdf", "application/pdf", 1024, UserId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.FileId);
        Assert.Equal(instruction.UploadUrl, result.Value.UploadUrl);
        Assert.Equal(instruction.ExpiredAt, result.Value.ExpiredAt);
        Assert.Equal(instruction.Headers, result.Value.Headers);
    }

    [Fact]
    public async Task HandleAsync_ValidFile_CallsStorageProviderWithGeneratedKey()
    {
        _storage.InitUploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new UploadInstruction("url", new Dictionary<string, string>(), DateTime.UtcNow));

        var command = new InitUploadCommand(ProjectId, "doc.pdf", "application/pdf", 1024, UserId);

        await _handler.HandleAsync(command);

        await _storage.Received(1).InitUploadAsync(
            Arg.Is<string>(key => key.StartsWith($"projects/{ProjectId}/users/{UserId}/")),
            "application/pdf",
            1024,
            _options.UploadExpirationMinutes,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DisallowedExtension_ReturnsValidationFailure()
    {
        var command = new InitUploadCommand(ProjectId, "virus.exe", "application/octet-stream", 1024, UserId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_FILE_TYPE", result.FirstError!.Code);
        await _storage.DidNotReceive().InitUploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExceedsMaxSize_ReturnsValidationFailure()
    {
        var command = new InitUploadCommand(ProjectId, "big.pdf", "application/pdf",
            _options.MaxUploadSizeBytes + 1, UserId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_FILE_SIZE", result.FirstError!.Code);
        await _storage.DidNotReceive().InitUploadAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}

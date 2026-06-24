using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Handles a command that returns a value.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>
/// Handles a command with no return value.
/// </summary>
public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken ct = default);
}

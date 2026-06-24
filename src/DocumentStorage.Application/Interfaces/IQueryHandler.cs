using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Handles a query that returns a value (CQRS read side).
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}

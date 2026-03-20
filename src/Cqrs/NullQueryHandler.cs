namespace KatzuoOgust.Cqrs;

/// <summary>A no-op handler that returns <c>default</c> for any query.</summary>
public sealed class NullQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
	where TQuery : IQuery<TResponse>
{
	public static readonly NullQueryHandler<TQuery, TResponse> Instance = new();

	private NullQueryHandler() { }

	public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
		=> Task.FromResult<TResponse>(default!);
}

namespace KatzuoOgust.Cqrs;

/// <summary>Handles a query that produces <typeparamref name="TResponse"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse>
	where TQuery : IQuery<TResponse>
{
	/// <summary>Handles <paramref name="query"/> asynchronously and returns the result.</summary>
	/// <param name="query">The query to handle.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The result produced by handling <paramref name="query"/>.</returns>
	public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

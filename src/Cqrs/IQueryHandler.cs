namespace KatzuoOgust.Cqrs;

/// <summary>Handles a query that produces <typeparamref name="TResponse"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse>
	where TQuery : IQuery<TResponse>
{
	public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

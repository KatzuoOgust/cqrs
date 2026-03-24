namespace KatzuoOgust.Cqrs;

/// <summary>Dispatches a request to its single registered handler.</summary>
public interface IDispatcher
{
	/// <summary>Dispatches <paramref name="request"/> to its handler and returns the result.</summary>
	/// <typeparam name="TResponse">The result type produced by the handler.</typeparam>
	/// <param name="request">The request to dispatch.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The value returned by the handler.</returns>
	public Task<TResponse> InvokeAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

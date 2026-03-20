namespace KatzuoOgust.Cqrs;

/// <summary>Dispatches a request to its single registered handler.</summary>
public interface IDispatcher
{
	public Task<TResponse> InvokeAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

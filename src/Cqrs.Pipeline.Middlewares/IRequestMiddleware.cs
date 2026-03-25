namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

/// <summary>
/// A pipeline behavior that wraps handler execution.
/// Pipelines are invoked outermost-first; call <paramref name="next"/> to continue the chain.
/// </summary>
public interface IRequestMiddleware<TRequest, TResult>
	where TRequest : IRequest<TResult>
{
	/// <summary>
	/// Processes <paramref name="request"/> and delegates to <paramref name="next"/> to continue the chain.
	/// </summary>
	/// <param name="request">The request being dispatched.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <param name="next">The continuation delegate; must be called to continue the pipeline.</param>
	/// <returns>The result produced by the pipeline or handler.</returns>
#pragma warning disable CA1068 // CancellationToken intentionally precedes the next delegate so callers can pass ct into it
	public Task<TResult> HandleAsync(TRequest request, CancellationToken ct, RequestMiddlewareDelegate<TResult> next);
#pragma warning restore CA1068
}

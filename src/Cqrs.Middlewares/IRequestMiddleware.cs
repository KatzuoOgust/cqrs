using KatzuoOgust.Cqrs;
namespace KatzuoOgust.Cqrs.Middlewares;

/// <summary>
/// A pipeline behavior that wraps handler execution.
/// Pipelines are invoked outermost-first; call <paramref name="next"/> to continue the chain.
/// </summary>
public interface IRequestMiddleware<TRequest, TResult>
	where TRequest : IRequest<TResult>
{
	Task<TResult> HandleAsync(TRequest request, CancellationToken ct, Func<CancellationToken, Task<TResult>> next);
}

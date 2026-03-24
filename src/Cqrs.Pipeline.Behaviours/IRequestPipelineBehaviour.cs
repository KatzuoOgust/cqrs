namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

/// <summary>
/// A cross-cutting pipeline behaviour applied to every request.
/// Call <paramref name="next"/> to continue the chain toward the handler.
/// </summary>
public interface IRequestPipelineBehaviour
{
	/// <summary>
	/// Processes <paramref name="request"/> and delegates to <paramref name="next"/> to continue the chain.
	/// </summary>
	/// <param name="request">The non-generic request being dispatched.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <param name="next">The continuation delegate; must be called to continue the pipeline.</param>
	/// <returns>The result produced by the pipeline or handler, boxed as <see cref="object"/>.</returns>
	public Task<object?> HandleAsync(IRequest request, CancellationToken ct, Func<CancellationToken, Task<object?>> next);
}

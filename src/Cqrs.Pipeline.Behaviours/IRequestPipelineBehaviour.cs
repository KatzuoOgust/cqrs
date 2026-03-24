namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

/// <summary>
/// A cross-cutting pipeline behaviour applied to every request.
/// Call <paramref name="next"/> to continue the chain toward the handler.
/// </summary>
public interface IRequestPipelineBehaviour
{
	public Task<object?> HandleAsync(IRequest request, CancellationToken ct, Func<CancellationToken, Task<object?>> next);
}

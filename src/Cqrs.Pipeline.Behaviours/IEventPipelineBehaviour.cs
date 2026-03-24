namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

/// <summary>
/// A cross-cutting pipeline behaviour applied to every event publication.
/// Call <paramref name="next"/> to continue the chain toward the handlers.
/// </summary>
public interface IEventPipelineBehaviour
{
	/// <summary>
	/// Processes <paramref name="event"/> and delegates to <paramref name="next"/> to continue the chain.
	/// </summary>
	/// <param name="event">The event being published.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <param name="next">The continuation delegate; must be called to continue the pipeline.</param>
#pragma warning disable CA1068 // CancellationToken intentionally precedes the next delegate so callers can pass ct into it
	public Task HandleAsync(IEvent @event, CancellationToken ct, Func<CancellationToken, Task> next);
#pragma warning restore CA1068
}

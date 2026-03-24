namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

/// <summary>
/// A middleware behavior that wraps event handler execution.
/// Middlewares are invoked outermost-first; call <paramref name="next"/> to continue the chain.
/// </summary>
public interface IEventMiddleware<TEvent>
	where TEvent : IEvent
{
	/// <summary>
	/// Processes <paramref name="event"/> and delegates to <paramref name="next"/> to continue the chain.
	/// </summary>
	/// <param name="event">The event being dispatched.</param>
	/// <param name="ct">A token to cancel the operation.</param>
	/// <param name="next">The continuation delegate; must be called to continue the pipeline.</param>
	public Task HandleAsync(TEvent @event, CancellationToken ct, Func<CancellationToken, Task> next);
}

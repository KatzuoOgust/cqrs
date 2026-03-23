namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

/// <summary>
/// A middleware behavior that wraps event handler execution.
/// Middlewares are invoked outermost-first; call <paramref name="next"/> to continue the chain.
/// </summary>
public interface IEventMiddleware<TEvent>
	where TEvent : IEvent
{
	Task HandleAsync(TEvent @event, CancellationToken ct, Func<CancellationToken, Task> next);
}

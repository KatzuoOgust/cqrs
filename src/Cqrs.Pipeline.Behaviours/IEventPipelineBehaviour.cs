namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

/// <summary>
/// A cross-cutting pipeline behaviour applied to every event publication.
/// Call <paramref name="next"/> to continue the chain toward the handlers.
/// </summary>
public interface IEventPipelineBehaviour
{
	Task HandleAsync(IEvent @event, CancellationToken ct, Func<CancellationToken, Task> next);
}

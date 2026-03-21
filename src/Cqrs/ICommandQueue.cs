namespace KatzuoOgust.Cqrs;

/// <summary>Accepts void commands for deferred or immediate processing.</summary>
public interface ICommandQueue
{
	public Task EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
		where TCommand : ICommand;
}

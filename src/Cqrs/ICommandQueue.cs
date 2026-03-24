namespace KatzuoOgust.Cqrs;

/// <summary>Accepts void commands for deferred or immediate processing.</summary>
public interface ICommandQueue
{
	/// <summary>Enqueues <paramref name="command"/> for processing.</summary>
	/// <param name="command">The command to enqueue.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	public Task EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
		where TCommand : ICommand;
}

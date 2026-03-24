namespace KatzuoOgust.Cqrs;

/// <summary>Handles a command that produces no result.</summary>
public interface ICommandHandler<in TCommand>
	where TCommand : ICommand
{
	/// <summary>Handles <paramref name="command"/> asynchronously.</summary>
	/// <param name="command">The command to handle.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Handles a command that produces <typeparamref name="TResponse"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse>
	where TCommand : ICommand<TResponse>
{
	/// <summary>Handles <paramref name="command"/> asynchronously and returns the result.</summary>
	/// <param name="command">The command to handle.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The result produced by handling <paramref name="command"/>.</returns>
	public Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

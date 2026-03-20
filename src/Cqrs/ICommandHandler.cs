namespace KatzuoOgust.Cqrs;

/// <summary>Handles a command that produces no result.</summary>
public interface ICommandHandler<in TCommand>
	where TCommand : ICommand
{
	public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Handles a command that produces <typeparamref name="TResponse"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse>
	where TCommand : ICommand<TResponse>
{
	public Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

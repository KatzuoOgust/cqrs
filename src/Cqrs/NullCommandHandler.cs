namespace KatzuoOgust.Cqrs;

/// <summary>A no-op handler for commands that produce no result.</summary>
public sealed class NullCommandHandler<TCommand> : ICommandHandler<TCommand>
	where TCommand : ICommand
{
	public static readonly NullCommandHandler<TCommand> Instance = new();

	private NullCommandHandler() { }

	public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}

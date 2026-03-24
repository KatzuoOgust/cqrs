namespace KatzuoOgust.Cqrs;

/// <summary>A no-op handler for commands that produce no result.</summary>
public sealed class NullCommandHandler<TCommand> : ICommandHandler<TCommand>
	where TCommand : ICommand
{
	/// <summary>The shared singleton instance of <see cref="NullCommandHandler{TCommand}"/>.</summary>
	public static readonly NullCommandHandler<TCommand> Instance = new();

	private NullCommandHandler() { }

	/// <inheritdoc/>
	public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}

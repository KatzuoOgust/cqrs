namespace KatzuoOgust.Cqrs.Flow;

/// <summary>
/// Handles commands that produce a sequence of follow-up commands to be enqueued.
/// Enables command composition and workflow orchestration.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface IFlowCommandHandler<TCommand>
	where TCommand : ICommand
{
	/// <summary>
	/// Processes a command and returns a sequence of commands to be enqueued and executed next.
	/// </summary>
	/// <param name="command">The command to process.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A sequence of commands to enqueue; empty if no follow-up commands are needed.</returns>
	Task<IEnumerable<ICommand>> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default);
}

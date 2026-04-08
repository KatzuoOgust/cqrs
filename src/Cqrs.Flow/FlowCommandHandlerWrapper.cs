namespace KatzuoOgust.Cqrs.Flow;

/// <summary>
/// Adapter that wraps an <see cref="IFlowCommandHandler{TCommand}"/> as an <see cref="ICommandHandler{TCommand}"/>.
/// Executes the flow handler and enqueues any returned commands using the provided command queue.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public sealed class FlowCommandHandlerWrapper<TCommand> : ICommandHandler<TCommand>
	where TCommand : ICommand
{
	private readonly IFlowCommandHandler<TCommand> _flowHandler;
	private readonly ICommandQueue _commandQueue;

	/// <summary>
	/// Initializes a new instance of the <see cref="FlowCommandHandlerWrapper{TCommand}"/> class.
	/// </summary>
	/// <param name="flowHandler">The flow command handler to wrap.</param>
	/// <param name="commandQueue">The command queue used to enqueue returned commands.</param>
	/// <exception cref="ArgumentNullException">Thrown when either parameter is null.</exception>
	public FlowCommandHandlerWrapper(IFlowCommandHandler<TCommand> flowHandler, ICommandQueue commandQueue)
	{
		ArgumentNullException.ThrowIfNull(flowHandler);
		ArgumentNullException.ThrowIfNull(commandQueue);

		_flowHandler = flowHandler;
		_commandQueue = commandQueue;
	}

	/// <inheritdoc/>
	public async Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(command);

		var nextCommands = await _flowHandler.ExecuteAsync(command, cancellationToken);

		if (nextCommands != null)
		{
			foreach (var nextCommand in nextCommands)
			{
				if (nextCommand != null)
				{
					await _commandQueue.EnqueueAsync(nextCommand, cancellationToken);
				}
			}
		}
	}
}

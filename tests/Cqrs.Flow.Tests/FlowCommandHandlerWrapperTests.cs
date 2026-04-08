using KatzuoOgust.Cqrs.DependencyInjection;
using KatzuoOgust.Cqrs.Flow;

namespace KatzuoOgust.Cqrs.Flow;

public class FlowCommandHandlerWrapperTests
{
	[Fact]
	public void Constructor_ThrowsArgumentNullException_WithNullFlowHandler()
	{
		var queue = new TestCommandQueue();

		Assert.Throws<ArgumentNullException>(() => new FlowCommandHandlerWrapper<SimpleCommand>(null!, queue));
	}

	[Fact]
	public void Constructor_ThrowsArgumentNullException_WithNullCommandQueue()
	{
		var handler = new SimpleFlowCommandHandler();

		Assert.Throws<ArgumentNullException>(() => new FlowCommandHandlerWrapper<SimpleCommand>(handler, null!));
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WithNullCommand()
	{
		var handler = new SimpleFlowCommandHandler();
		var queue = new TestCommandQueue();
		var wrapper = new FlowCommandHandlerWrapper<SimpleCommand>(handler, queue);

		await Assert.ThrowsAsync<ArgumentNullException>(() => wrapper.HandleAsync(null!));
	}

	[Fact]
	public async Task HandleAsync_ExecutesFlowHandlerWithCommand()
	{
		var command = new SimpleCommand(42);
		var handler = new SimpleFlowCommandHandler();
		var queue = new TestCommandQueue();
		var wrapper = new FlowCommandHandlerWrapper<SimpleCommand>(handler, queue);

		await wrapper.HandleAsync(command);

		Assert.Same(command, handler.ReceivedCommand);
	}

	[Fact]
	public async Task HandleAsync_EnqueuesReturnedCommands()
	{
		var command = new SimpleCommand(1);
		var followUp1 = new SimpleCommand(2);
		var followUp2 = new SimpleCommand(3);

		var handler = new SimpleFlowCommandHandler()
			.WithFollowUp(followUp1)
			.WithFollowUp(followUp2);

		var queue = new TestCommandQueue();
		var wrapper = new FlowCommandHandlerWrapper<SimpleCommand>(handler, queue);

		await wrapper.HandleAsync(command);

		Assert.Equal(2, queue.EnqueuedCommands.Count);
		Assert.Same(followUp1, queue.EnqueuedCommands[0]);
		Assert.Same(followUp2, queue.EnqueuedCommands[1]);
	}

	[Fact]
	public async Task HandleAsync_SkipsNullFollowUpCommands()
	{
		var command = new SimpleCommand(1);
		var followUp = new SimpleCommand(2);

		var handler = new SimpleFlowCommandHandler()
			.WithFollowUp(followUp)
			.WithFollowUp(null)
			.WithFollowUp(followUp);

		var queue = new TestCommandQueue();
		var wrapper = new FlowCommandHandlerWrapper<SimpleCommand>(handler, queue);

		await wrapper.HandleAsync(command);

		Assert.Equal(2, queue.EnqueuedCommands.Count);
		Assert.Same(followUp, queue.EnqueuedCommands[0]);
		Assert.Same(followUp, queue.EnqueuedCommands[1]);
	}

	[Fact]
	public async Task HandleAsync_RespectsProvidedCancellationToken()
	{
		var command = new SimpleCommand(1);
		var cts = new CancellationTokenSource();
		var handler = new SimpleFlowCommandHandler();
		var queue = new TestCommandQueue();
		var wrapper = new FlowCommandHandlerWrapper<SimpleCommand>(handler, queue);

		await wrapper.HandleAsync(command, cts.Token);

		Assert.Equal(cts.Token, handler.ReceivedCancellationToken);
	}

	#region Fakes

	private sealed record SimpleCommand(int Value) : ICommand;

	private sealed class SimpleFlowCommandHandler : IFlowCommandHandler<SimpleCommand>
	{
		private readonly List<ICommand?> _followUpCommands = [];

		public SimpleCommand? ReceivedCommand { get; private set; }
		public CancellationToken ReceivedCancellationToken { get; private set; }

		public SimpleFlowCommandHandler WithFollowUp(ICommand? command)
		{
			_followUpCommands.Add(command);
			return this;
		}

		public Task<IEnumerable<ICommand>> ExecuteAsync(SimpleCommand command, CancellationToken cancellationToken = default)
		{
			ReceivedCommand = command;
			ReceivedCancellationToken = cancellationToken;

			var result = _followUpCommands
				.Where(c => c != null)
				.Cast<ICommand>()
				.ToList();

			return Task.FromResult<IEnumerable<ICommand>>(result);
		}
	}

	private sealed class TestCommandQueue : ICommandQueue
	{
		public List<ICommand> EnqueuedCommands { get; } = [];

		public async Task EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
			where TCommand : ICommand
		{
			EnqueuedCommands.Add(command);
			await Task.CompletedTask;
		}
	}

	#endregion
}

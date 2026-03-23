using KatzuoOgust.Cqrs;
namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

public sealed class BehaviourAwareDispatcherTests
{
	// -----------------------------------------------------------------------
	// Fixtures
	// -----------------------------------------------------------------------

	private sealed record AddCommand(int A, int B) : ICommand<int>;
	private sealed record PingCommand : ICommand;

	private sealed class AddHandler : ICommandHandler<AddCommand, int>
	{
		public Task<int> HandleAsync(AddCommand command, CancellationToken ct = default) =>
			Task.FromResult(command.A + command.B);
	}

	private sealed class PingHandler : ICommandHandler<PingCommand>
	{
		public bool Invoked { get; private set; }
		public Task HandleAsync(PingCommand command, CancellationToken ct = default)
		{
			Invoked = true;
			return Task.CompletedTask;
		}
	}

	private sealed class LoggingBehaviour(List<string> log, string name) : IRequestPipelineBehaviour
	{
		public async Task<object?> HandleAsync(IRequest request, CancellationToken ct, Func<CancellationToken, Task<object?>> next)
		{
			log.Add($"{name}:before");
			var result = await next(ct);
			log.Add($"{name}:after");
			return result;
		}
	}

	private sealed class SimpleServiceProvider : IServiceProvider
	{
		private readonly Dictionary<Type, object> _services = [];
		public void Register<T>(T impl) where T : class => _services[typeof(T)] = impl;
		public object? GetService(Type t) => _services.GetValueOrDefault(t);
	}

	// -----------------------------------------------------------------------
	// Tests
	// -----------------------------------------------------------------------

	[Fact]
	public async Task InvokeAsync_PassesThroughToHandler_WhenNoBehaviours()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());

		var result = await new BehaviourAwareDispatcher(new Dispatcher(sp), sp).InvokeAsync(new AddCommand(3, 4));

		Assert.Equal(7, result);
	}

	[Fact]
	public async Task InvokeAsync_WrapsDispatch_WhenSingleBehaviour()
	{
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());
		sp.Register<IEnumerable<IRequestPipelineBehaviour>>([new LoggingBehaviour(log, "b")]);

		var result = await new BehaviourAwareDispatcher(new Dispatcher(sp), sp).InvokeAsync(new AddCommand(2, 3));

		Assert.Equal(5, result);
		Assert.Equal(["b:before", "b:after"], log);
	}

	[Fact]
	public async Task InvokeAsync_ChainsOutermostFirst_WhenMultipleBehaviours()
	{
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());
		sp.Register<IEnumerable<IRequestPipelineBehaviour>>([
			new LoggingBehaviour(log, "b0"),
			new LoggingBehaviour(log, "b1"),
		]);

		await new BehaviourAwareDispatcher(new Dispatcher(sp), sp).InvokeAsync(new AddCommand(1, 1));

		Assert.Equal(["b0:before", "b1:before", "b1:after", "b0:after"], log);
	}

	[Fact]
	public async Task InvokeAsync_AppliesBehaviour_WhenVoidCommand()
	{
		var log = new List<string>();
		var handler = new PingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(handler);
		sp.Register<IEnumerable<IRequestPipelineBehaviour>>([new LoggingBehaviour(log, "b")]);

		await new BehaviourAwareDispatcher(new Dispatcher(sp), sp).InvokeAsync(new PingCommand());

		Assert.True(handler.Invoked);
		Assert.Equal(["b:before", "b:after"], log);
	}

	[Fact]
	public async Task InvokeAsync_PassesRequestToBehaviour()
	{
		IRequest? captured = null;
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());
		sp.Register<IEnumerable<IRequestPipelineBehaviour>>([new CapturingBehaviour(r => captured = r)]);

		await new BehaviourAwareDispatcher(new Dispatcher(sp), sp).InvokeAsync(new AddCommand(1, 2));

		Assert.IsType<AddCommand>(captured);
	}

	[Fact]
	public async Task InvokeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new BehaviourAwareDispatcher(new Dispatcher(new SimpleServiceProvider()), new SimpleServiceProvider())
				.InvokeAsync<Unit>(null!));
	}

	private sealed class CapturingBehaviour(Action<IRequest> capture) : IRequestPipelineBehaviour
	{
		public async Task<object?> HandleAsync(IRequest request, CancellationToken ct, Func<CancellationToken, Task<object?>> next)
		{
			capture(request);
			return await next(ct);
		}
	}
}

namespace KatzuoOgust.Cqrs;

public sealed class DispatcherTests
{
	// -----------------------------------------------------------------------
	// Test fixtures
	// -----------------------------------------------------------------------

	private sealed record PingCommand : ICommand;
	private sealed record EchoCommand(string Text) : ICommand<string>;
	private sealed record LengthQuery(string Text) : IQuery<int>;

	private sealed class PingHandler : ICommandHandler<PingCommand>
	{
		public bool Invoked { get; private set; }

		public Task HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
		{
			Invoked = true;
			return Task.CompletedTask;
		}
	}

	private sealed class EchoHandler : ICommandHandler<EchoCommand, string>
	{
		public Task<string> HandleAsync(EchoCommand command, CancellationToken cancellationToken = default) =>
			Task.FromResult(command.Text);
	}

	private sealed class LengthHandler : IQueryHandler<LengthQuery, int>
	{
		public Task<int> HandleAsync(LengthQuery query, CancellationToken cancellationToken = default) =>
			Task.FromResult(query.Text.Length);
	}

	private sealed class SimpleServiceProvider : IServiceProvider
	{
		private readonly Dictionary<Type, object> _services = [];

		public void Register<TService>(TService impl) where TService : class =>
			_services[typeof(TService)] = impl;

		public object? GetService(Type serviceType) =>
			_services.GetValueOrDefault(serviceType);
	}

	// -----------------------------------------------------------------------
	// Tests
	// -----------------------------------------------------------------------

	[Fact]
	public async Task InvokeAsync_VoidCommand_InvokesHandlerAndReturnsUnit()
	{
		var handler = new PingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(handler);

		var result = await new Dispatcher(sp).InvokeAsync(new PingCommand());

		Assert.Equal(Unit.Value, result);
		Assert.True(handler.Invoked);
	}

	[Fact]
	public async Task InvokeAsync_CommandWithResult_ReturnsHandlerResult()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<EchoCommand, string>>(new EchoHandler());

		var result = await new Dispatcher(sp).InvokeAsync(new EchoCommand("hello"));

		Assert.Equal("hello", result);
	}

	[Fact]
	public async Task InvokeAsync_Query_ReturnsHandlerResult()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IQueryHandler<LengthQuery, int>>(new LengthHandler());

		var result = await new Dispatcher(sp).InvokeAsync(new LengthQuery("hello"));

		Assert.Equal(5, result);
	}

	[Fact]
	public async Task InvokeAsync_UnregisteredHandler_ThrowsInvalidOperationException()
	{
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(
			() => new Dispatcher(new SimpleServiceProvider()).InvokeAsync(new PingCommand()));

		Assert.Contains(nameof(PingCommand), ex.Message);
	}

	[Fact]
	public async Task InvokeAsync_NullRequest_ThrowsArgumentNullException()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new Dispatcher(new SimpleServiceProvider()).InvokeAsync<Unit>(null!));
	}

	[Fact]
	public void Ctor_NullServiceProvider_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new Dispatcher(null!));
	}

	[Fact]
	public async Task InvokeAsync_CalledMultipleTimes_UsesCachedProcessor()
	{
		var handler = new PingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(handler);
		var dispatcher = new Dispatcher(sp);

		// Both calls must succeed — the second one hits the cached compiled delegate.
		await dispatcher.InvokeAsync(new PingCommand());
		await dispatcher.InvokeAsync(new PingCommand());

		Assert.True(handler.Invoked);
	}

	[Fact]
	public async Task InvokeAsync_VoidCommand_CancellationTokenForwarded()
	{
		var captured = CancellationToken.None;
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(new CapturingHandler(t => captured = t));

		using var cts = new CancellationTokenSource();
		await new Dispatcher(sp).InvokeAsync(new PingCommand(), cts.Token);

		Assert.Equal(cts.Token, captured);
	}

	private sealed class CapturingHandler(Action<CancellationToken> capture) : ICommandHandler<PingCommand>
	{
		public Task HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
		{
			capture(cancellationToken);
			return Task.CompletedTask;
		}
	}

	// -----------------------------------------------------------------------
	// ICommandQueue
	// -----------------------------------------------------------------------

	[Fact]
	public async Task EnqueueAsync_VoidCommand_InvokesHandler()
	{
		var handler = new PingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(handler);

		await new Dispatcher(sp).EnqueueAsync(new PingCommand());

		Assert.True(handler.Invoked);
	}

	[Fact]
	public async Task EnqueueAsync_CancellationTokenForwarded()
	{
		var captured = CancellationToken.None;
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(new CapturingHandler(t => captured = t));

		using var cts = new CancellationTokenSource();
		await new Dispatcher(sp).EnqueueAsync(new PingCommand(), cts.Token);

		Assert.Equal(cts.Token, captured);
	}

	[Fact]
	public void Dispatcher_ImplementsICommandQueue()
	{
		Assert.IsAssignableFrom<ICommandQueue>(new Dispatcher(new SimpleServiceProvider()));
	}
}

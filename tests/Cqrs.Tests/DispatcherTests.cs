using KatzuoOgust.Cqrs.DependencyInjection;

namespace KatzuoOgust.Cqrs;

public sealed partial class DispatcherTests
{
	// -----------------------------------------------------------------------
	// Tests
	// -----------------------------------------------------------------------

	[Fact]
	public async Task InvokeAsync_ReturnsUnitAndInvokesHandler_WhenVoidCommand()
	{
		var handler = new PingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(handler);

		var result = await new Dispatcher(sp).InvokeAsync(new PingCommand());

		Assert.Equal(Unit.Value, result);
		Assert.True(handler.Invoked);
	}

	[Fact]
	public async Task InvokeAsync_ReturnsHandlerResult_WhenCommandWithResult()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<EchoCommand, string>>(new EchoHandler());

		var result = await new Dispatcher(sp).InvokeAsync(new EchoCommand("hello"));

		Assert.Equal("hello", result);
	}

	[Fact]
	public async Task InvokeAsync_ReturnsHandlerResult_WhenQuery()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IQueryHandler<LengthQuery, int>>(new LengthHandler());

		var result = await new Dispatcher(sp).InvokeAsync(new LengthQuery("hello"));

		Assert.Equal(5, result);
	}

	[Fact]
	public async Task InvokeAsync_ThrowsInvalidOperationException_WhenHandlerNotRegistered()
	{
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(
			() => new Dispatcher(new SimpleServiceProvider()).InvokeAsync(new PingCommand()));

		Assert.Contains(nameof(PingCommand), ex.Message);
	}

	[Fact]
	public async Task InvokeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new Dispatcher(new SimpleServiceProvider()).InvokeAsync<Unit>(null!));
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenServiceProviderIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => new Dispatcher(null!));
	}

	[Fact]
	public async Task InvokeAsync_UsesCachedDelegate_WhenCalledMultipleTimes()
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
	public async Task InvokeAsync_ForwardsCancellationToken_WhenVoidCommand()
	{
		var captured = CancellationToken.None;
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(new CapturingHandler(t => captured = t));

		using var cts = new CancellationTokenSource();
		await new Dispatcher(sp).InvokeAsync(new PingCommand(), cts.Token);

		Assert.Equal(cts.Token, captured);
	}

	// -----------------------------------------------------------------------
	// ICommandQueue
	// -----------------------------------------------------------------------

	[Fact]
	public async Task EnqueueAsync_InvokesHandler_WhenVoidCommand()
	{
		var handler = new PingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<PingCommand>>(handler);

		await new Dispatcher(sp).EnqueueAsync(new PingCommand());

		Assert.True(handler.Invoked);
	}

	[Fact]
	public async Task EnqueueAsync_ForwardsCancellationToken()
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

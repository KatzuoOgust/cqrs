namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

public sealed partial class MiddlewareAwareDispatcherTests
{
	// -----------------------------------------------------------------------
	// Tests
	// -----------------------------------------------------------------------

	[Fact]
	public async Task InvokeAsync_PassesThroughToHandler_WhenNoMiddlewares()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());

		var dispatcher = new MiddlewareAwareDispatcher(new Dispatcher(sp), sp);

		Assert.Equal(7, await dispatcher.InvokeAsync(new AddCommand(3, 4)));
	}

	[Fact]
	public async Task InvokeAsync_WrapsResult_WhenSingleMiddleware()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());
		sp.Register<IEnumerable<IRequestMiddleware<AddCommand, int>>>(
			[new MultiplierMiddleware([], "p", factor: 10)]);

		var result = await new MiddlewareAwareDispatcher(new Dispatcher(sp), sp)
			.InvokeAsync(new AddCommand(2, 3)); // (2+3)*10

		Assert.Equal(50, result);
	}

	[Fact]
	public async Task InvokeAsync_ChainsOutermostFirst_WhenMultipleMiddlewares()
	{
		// pipeline[0] is outermost: multiplies by 10 AFTER pipeline[1] (×2) and handler (+)
		// order: p0.before → p1.before → handler → p1.after → p0.after
		// result: handler=5, ×2=10, ×10=100
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());
		sp.Register<IEnumerable<IRequestMiddleware<AddCommand, int>>>([
			new MultiplierMiddleware(log, "p0", factor: 10),
			new MultiplierMiddleware(log, "p1", factor: 2),
		]);

		var result = await new MiddlewareAwareDispatcher(new Dispatcher(sp), sp)
			.InvokeAsync(new AddCommand(2, 3));

		Assert.Equal(100, result);
		Assert.Equal(["p0:before", "p1:before", "p1:after", "p0:after"], log);
	}

	[Fact]
	public async Task InvokeAsync_AppliesMiddleware_WhenQuery()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<IQueryHandler<MultiplyQuery, int>>(new MultiplyHandler());
		sp.Register<IEnumerable<IRequestMiddleware<MultiplyQuery, int>>>(
			[new Doubling()]);

		var result = await new MiddlewareAwareDispatcher(new Dispatcher(sp), sp)
			.InvokeAsync(new MultiplyQuery(3, 4)); // 3*4=12, doubled=24

		Assert.Equal(24, result);
	}

	[Fact]
	public async Task InvokeAsync_AppliesMiddleware_WhenVoidCommand()
	{
		var handler = new LogHandler();
		var pipeline = new VoidPipeline();
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<LogCommand>>(handler);
		sp.Register<IEnumerable<IRequestMiddleware<LogCommand, Unit>>>([pipeline]);

		await new MiddlewareAwareDispatcher(new Dispatcher(sp), sp)
			.InvokeAsync(new LogCommand());

		Assert.True(handler.Invoked);
		Assert.True(pipeline.Invoked);
	}

	[Fact]
	public async Task InvokeAsync_UsesCachedInvoker_WhenCalledMultipleTimes()
	{
		var sp = new SimpleServiceProvider();
		sp.Register<ICommandHandler<AddCommand, int>>(new AddHandler());

		var dispatcher = new MiddlewareAwareDispatcher(new Dispatcher(sp), sp);
		Assert.Equal(5, await dispatcher.InvokeAsync(new AddCommand(2, 3)));
		Assert.Equal(9, await dispatcher.InvokeAsync(new AddCommand(4, 5)));
	}

}

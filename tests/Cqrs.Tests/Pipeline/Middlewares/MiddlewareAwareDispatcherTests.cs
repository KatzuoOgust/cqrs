namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

public sealed class MiddlewareAwareDispatcherTests
{
	// -----------------------------------------------------------------------
	// Fixtures
	// -----------------------------------------------------------------------

	private sealed record AddCommand(int A, int B) : ICommand<int>;
	private sealed record MultiplyQuery(int A, int B) : IQuery<int>;
	private sealed record LogCommand : ICommand;

	private sealed class AddHandler : ICommandHandler<AddCommand, int>
	{
		public Task<int> HandleAsync(AddCommand command, CancellationToken ct = default) =>
			Task.FromResult(command.A + command.B);
	}

	private sealed class MultiplyHandler : IQueryHandler<MultiplyQuery, int>
	{
		public Task<int> HandleAsync(MultiplyQuery query, CancellationToken ct = default) =>
			Task.FromResult(query.A * query.B);
	}

	private sealed class LogHandler : ICommandHandler<LogCommand>
	{
		public bool Invoked { get; private set; }
		public Task HandleAsync(LogCommand command, CancellationToken ct = default)
		{
			Invoked = true;
			return Task.CompletedTask;
		}
	}

	/// <summary>Records invocation order and multiplies the result by <paramref name="factor"/>.</summary>
	private sealed class MultiplierMiddleware(List<string> log, string name, int factor)
		: IRequestMiddleware<AddCommand, int>
	{
		public async Task<int> HandleAsync(AddCommand request, CancellationToken ct, Func<CancellationToken, Task<int>> next)
		{
			log.Add($"{name}:before");
			var result = await next(ct);
			log.Add($"{name}:after");
			return result * factor;
		}
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

	private sealed class Doubling : IRequestMiddleware<MultiplyQuery, int>
	{
		public async Task<int> HandleAsync(MultiplyQuery request, CancellationToken ct, Func<CancellationToken, Task<int>> next) =>
			await next(ct) * 2;
	}

	private sealed class VoidPipeline : IRequestMiddleware<LogCommand, Unit>
	{
		public bool Invoked { get; private set; }

		public async Task<Unit> HandleAsync(LogCommand request, CancellationToken ct, Func<CancellationToken, Task<Unit>> next)
		{
			Invoked = true;
			return await next(ct);
		}
	}
}

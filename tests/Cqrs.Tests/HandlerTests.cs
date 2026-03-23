namespace KatzuoOgust.Cqrs;

public sealed class NullCommandHandlerTests
{
	private sealed record TestCommand : ICommand;

	[Fact]
	public async Task HandleAsync_Completes()
	{
		await NullCommandHandler<TestCommand>.Instance.HandleAsync(new TestCommand());
	}

	[Fact]
	public async Task HandleAsync_ThrowsOperationCanceledException_WhenCancellationRequested()
	{
		using var cts = new CancellationTokenSource();
		await NullCommandHandler<TestCommand>.Instance.HandleAsync(new TestCommand(), cts.Token);
	}

	[Fact]
	public void Instance_ReturnsSameInstance()
	{
		Assert.Same(
			NullCommandHandler<TestCommand>.Instance,
			NullCommandHandler<TestCommand>.Instance);
	}
}

public sealed class NullQueryHandlerTests
{
	private sealed record TestQuery : IQuery<string>;
	private sealed record IntQuery : IQuery<int>;

	[Fact]
	public async Task HandleAsync_ReturnsDefault_WhenResponseIsReferenceType()
	{
		var result = await NullQueryHandler<TestQuery, string>.Instance.HandleAsync(new TestQuery());
		Assert.Null(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsDefault_WhenResponseIsValueType()
	{
		var result = await NullQueryHandler<IntQuery, int>.Instance.HandleAsync(new IntQuery());
		Assert.Equal(0, result);
	}

	[Fact]
	public void Instance_ReturnsSameInstance()
	{
		Assert.Same(
			NullQueryHandler<TestQuery, string>.Instance,
			NullQueryHandler<TestQuery, string>.Instance);
	}
}

public sealed class NullEventHandlerTests
{
	private sealed record TestEvent : IEvent;

	[Fact]
	public async Task HandleAsync_Completes()
	{
		await NullEventHandler<TestEvent>.Instance.HandleAsync(new TestEvent());
	}

	[Fact]
	public async Task HandleAsync_ThrowsOperationCanceledException_WhenCancellationRequested()
	{
		using var cts = new CancellationTokenSource();
		await NullEventHandler<TestEvent>.Instance.HandleAsync(new TestEvent(), cts.Token);
	}

	[Fact]
	public void Instance_ReturnsSameInstance()
	{
		Assert.Same(
			NullEventHandler<TestEvent>.Instance,
			NullEventHandler<TestEvent>.Instance);
	}
}

public sealed class UnitTests
{
	[Fact]
	public void Value_EqualsOtherUnitInstance()
	{
		Assert.Equal(Unit.Value, Unit.Value);
	}

	[Fact]
	public void ToString_ReturnsParentheses()
	{
		Assert.Equal("()", Unit.Value.ToString());
	}

	[Fact]
	public void EqualityOperators_ReturnExpected()
	{
		Unit a = Unit.Value;
		Unit b = Unit.Value;
		Assert.True(a == b);
		Assert.False(a != b);
	}
}

namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

/// <summary>
/// Wraps an <see cref="IServiceProvider"/> and applies registered decorators on top of resolved services.
/// Decorators are applied lazily at resolution time in registration order (first registered = innermost wrap).
/// Multiple decorators may be registered for the same service type.
/// Use <see cref="DecoratingServiceProviderExtensions"/> to register decorators.
/// </summary>
public sealed class DecoratingServiceProvider : IServiceProvider
{
	private readonly IServiceProvider _inner;
	private readonly List<Decorator> _decorators = [];

	/// <summary>
	/// Initializes a new instance of <see cref="DecoratingServiceProvider"/>.
	/// </summary>
	/// <param name="inner">The underlying <see cref="IServiceProvider"/> to resolve services from.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is <see langword="null"/>.</exception>
	public DecoratingServiceProvider(IServiceProvider inner)
	{
		ArgumentNullException.ThrowIfNull(inner);
		_inner = inner;
	}

	/// <summary>
	/// Appends <paramref name="descriptor"/> to the decoration pipeline.
	/// Descriptors are applied in the order they are added: the first added wraps the raw inner service,
	/// each subsequent one wraps the result of the previous.
	/// </summary>
	public void Add(Decorator descriptor)
	{
		ArgumentNullException.ThrowIfNull(descriptor);
		_decorators.Add(descriptor);
	}

	/// <summary>
	/// Returns the decorators registered so far, in registration order.
	/// Primarily used by <see cref="DecoratingServiceProviderExtensions.When"/> to lift descriptors
	/// from a collector provider into conditional wrappers.
	/// </summary>
	public IReadOnlyList<Decorator> Decorators => _decorators;

	/// <inheritdoc/>
	public object? GetService(Type serviceType)
	{
		ArgumentNullException.ThrowIfNull(serviceType);

		var service = _inner.GetService(serviceType);
		if (service is null) return null;

		var openType = serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : null;

		foreach (var descriptor in _decorators)
			service = descriptor.TryApply(serviceType, openType, service, this) ?? service;

		return service;
	}
}

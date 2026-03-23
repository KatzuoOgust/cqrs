namespace KatzuoOgust.Cqrs.DependencyInjection;

/// <summary>
/// Minimal <see cref="IServiceProvider"/> for tests and examples — no external DI framework required.
/// Supports exact-type resolution and <see cref="IEnumerable{T}"/> resolution for middleware/behaviour chains.
/// </summary>
public sealed class SimpleServiceProvider : IServiceProvider
{
	private readonly Dictionary<Type, Func<object>> _singles = new();
	private readonly Dictionary<Type, List<Func<object>>> _collections = new();

	/// <summary>Registers a single instance for <typeparamref name="T"/>.</summary>
	public SimpleServiceProvider Register<T>(T instance) where T : class
	{
		_singles[typeof(T)] = () => instance;
		return this;
	}

	/// <summary>Registers a factory for <typeparamref name="T"/>. The factory is invoked on each <see cref="GetService"/> call.</summary>
	public SimpleServiceProvider Register<T>(Func<T> factory) where T : class
	{
		_singles[typeof(T)] = factory;
		return this;
	}

	/// <summary>
	/// Registers an instance into the ordered list for <typeparamref name="T"/>.
	/// Resolved as <c>IEnumerable&lt;T&gt;</c> in registration order.
	/// </summary>
	public SimpleServiceProvider RegisterMany<T>(T instance) where T : class
	{
		if (!_collections.TryGetValue(typeof(T), out var list))
			_collections[typeof(T)] = list = [];
		list.Add(() => instance);
		return this;
	}

	/// <summary>
	/// Registers a factory into the ordered list for <typeparamref name="T"/>.
	/// Resolved as <c>IEnumerable&lt;T&gt;</c> in registration order. The factory is invoked on each <see cref="GetService"/> call.
	/// </summary>
	public SimpleServiceProvider RegisterMany<T>(Func<T> factory) where T : class
	{
		if (!_collections.TryGetValue(typeof(T), out var list))
			_collections[typeof(T)] = list = [];
		list.Add(factory);
		return this;
	}

	/// <inheritdoc/>
	public object? GetService(Type serviceType)
	{
		if (_singles.TryGetValue(serviceType, out var factory))
			return factory();

		if (serviceType.IsGenericType
			&& serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			var elemType = serviceType.GetGenericArguments()[0];
			var factories = _collections.GetValueOrDefault(elemType) ?? [];
			var arr = Array.CreateInstance(elemType, factories.Count);
			for (var i = 0; i < factories.Count; i++)
				arr.SetValue(factories[i](), i);
			return arr;
		}

		return null;
	}
}

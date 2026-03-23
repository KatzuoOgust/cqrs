namespace KatzuoOgust.Cqrs.Examples;

/// <summary>
/// Minimal service provider for examples — no external DI framework required.
/// Supports exact-type resolution and IEnumerable&lt;T&gt; resolution for middleware/behaviour chains.
/// </summary>
internal sealed class SimpleServiceProvider : IServiceProvider
{
	private readonly Dictionary<Type, Func<object>> _singles = new();
	private readonly Dictionary<Type, List<Func<object>>> _collections = new();

	/// <summary>Registers a single instance for type <typeparamref name="T"/>.</summary>
	public SimpleServiceProvider Register<T>(T instance) where T : class
	{
		_singles[typeof(T)] = () => instance;
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

	public object? GetService(Type serviceType)
	{
		if (serviceType.IsGenericType &&
		    serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			var elemType = serviceType.GetGenericArguments()[0];
			var factories = _collections.GetValueOrDefault(elemType) ?? [];
			var arr = Array.CreateInstance(elemType, factories.Count);
			for (var i = 0; i < factories.Count; i++)
				arr.SetValue(factories[i](), i);
			return arr;
		}

		return _singles.TryGetValue(serviceType, out var factory) ? factory() : null;
	}
}

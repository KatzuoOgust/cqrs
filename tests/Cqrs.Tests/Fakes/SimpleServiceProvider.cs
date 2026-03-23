namespace KatzuoOgust.Cqrs;

internal sealed class SimpleServiceProvider : IServiceProvider
{
	private readonly Dictionary<Type, object> _services = [];

	public void Register<T>(T impl) where T : class => _services[typeof(T)] = impl;

	public object? GetService(Type t) => _services.GetValueOrDefault(t);
}

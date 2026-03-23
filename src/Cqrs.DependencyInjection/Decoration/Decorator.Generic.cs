using System.Collections.Concurrent;

namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

public abstract partial class Decorator
{
	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type's
	/// open-generic definition matches <paramref name="openGenericServiceType"/>.
	/// The factory receives the closed service type, the inner instance, and the service provider.
	/// </summary>
	public static Decorator Generic(
		Type openGenericServiceType,
		Func<Type, object, IServiceProvider, object> factory) =>
		new GenericDecorator(openGenericServiceType, factory);

	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type's
	/// open-generic definition matches <paramref name="openGenericServiceType"/>.
	/// The factory receives the closed service type and the inner instance.
	/// </summary>
	public static Decorator Generic(
		Type openGenericServiceType,
		Func<Type, object, object> factory) =>
		new GenericDecorator(openGenericServiceType, factory);

	/// <summary>
	/// Creates a descriptor for open-generic decoration. Whenever a closed type whose open-generic
	/// definition matches <paramref name="openServiceType"/> is resolved, the corresponding closed
	/// <paramref name="openDecoratorType"/> is instantiated. Constructor invokers are compiled once
	/// per closed type and cached.
	/// Both arguments must be open generic type definitions (e.g. <c>typeof(ICommandHandler&lt;&gt;)</c>).
	/// </summary>
	public static Decorator GenericByType(
		Type openServiceType,
		Type openDecoratorType) =>
		new GenericDecorator(openServiceType, openDecoratorType);

	private sealed class GenericDecorator(
		Type openGenericServiceType,
		Func<Type, object, IServiceProvider, object> factory)
		: Decorator
	{
		public GenericDecorator(Type openGenericServiceType, Func<Type, object, object> factory)
			: this(openGenericServiceType, (type, svc, _) => factory(type, svc)) { }

		public GenericDecorator(Type openGenericServiceType, Type openDecoratorType)
			: this(openGenericServiceType, BuildCachingFactory(openDecoratorType)) { }

		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp)
			=> openServiceType == openGenericServiceType ? factory(serviceType, service, sp) : null;

		private static Func<Type, object, IServiceProvider, object> BuildCachingFactory(Type openDecoratorType)
		{
			var cache = new ConcurrentDictionary<Type, Func<object, IServiceProvider, object>>();
			return (serviceType, service, sp) =>
			{
				var factory = cache.GetOrAdd(serviceType, closedSvcType =>
				{
					var closedDecorator = openDecoratorType.MakeGenericType(closedSvcType.GetGenericArguments());
					return BuildCtorInvoker(closedSvcType, closedDecorator);
				});
				return factory(service, sp);
			};
		}
	}
}

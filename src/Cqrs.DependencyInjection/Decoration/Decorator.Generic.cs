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
		Func<Type, object, IServiceProvider, object> factory)
	{
		ArgumentNullException.ThrowIfNull(openGenericServiceType);
		ArgumentNullException.ThrowIfNull(factory);
		Error.ThrowIfNotOpenGenericTypeDefinition(openGenericServiceType);

		return new GenericDecorator(openGenericServiceType, factory);
	}

	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type's
	/// open-generic definition matches <paramref name="openGenericServiceType"/>.
	/// The factory receives the closed service type and the inner instance.
	/// </summary>
	public static Decorator Generic(
		Type openGenericServiceType,
		Func<Type, object, object> factory)
	{
		ArgumentNullException.ThrowIfNull(openGenericServiceType);
		ArgumentNullException.ThrowIfNull(factory);
		Error.ThrowIfNotOpenGenericTypeDefinition(openGenericServiceType);

		return new GenericDecorator(openGenericServiceType, (svcType, svc, _) => factory(svcType, svc));
	}

	/// <summary>
	/// Creates a descriptor for open-generic decoration. Whenever a closed type whose open-generic
	/// definition matches <paramref name="openServiceType"/> is resolved, the corresponding closed
	/// <paramref name="openDecoratorType"/> is instantiated. Constructor invokers are compiled once
	/// per closed type and cached.
	/// Both arguments must be open generic type definitions (e.g. <c>typeof(ICommandHandler&lt;&gt;)</c>).
	/// </summary>
	public static Decorator Generic(
		Type openServiceType,
		Type openDecoratorType)
	{
		ArgumentNullException.ThrowIfNull(openServiceType);
		ArgumentNullException.ThrowIfNull(openDecoratorType);
		Error.ThrowIfNotOpenGenericTypeDefinition(openServiceType);
		Error.ThrowIfNotOpenGenericTypeDefinition(openDecoratorType);

		return new GenericDecorator(openServiceType, BuildCachingFactory(openDecoratorType));

		static Func<Type, object, IServiceProvider, object> BuildCachingFactory(Type openDecoratorType)
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

	private sealed class GenericDecorator(
		Type openGenericServiceType,
		Func<Type, object, IServiceProvider, object> factory)
		: Decorator
	{
		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp)
			=> openServiceType == openGenericServiceType ? factory(serviceType, service, sp) : null;
	}
}

using System.Linq.Expressions;

namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

/// <summary>
/// Describes a single decorator registration. Subclasses are produced by the static factory methods;
/// consumers should never instantiate them directly.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TryApply"/> is called for every service resolved by <see cref="DecoratingServiceProvider"/>.
/// Return a non-<see langword="null"/> value to wrap the service; return <see langword="null"/> to leave it
/// unchanged (i.e. this descriptor does not apply to the given type).
/// </para>
/// <para>
/// Descriptors are applied in registration order: the first registered descriptor wraps the raw inner
/// service, each subsequent one wraps the result of the previous.
/// </para>
/// </remarks>
public abstract partial class Decorator
{
	/// <summary>
	/// Attempts to apply this decorator to <paramref name="service"/>.
	/// </summary>
	/// <param name="serviceType">The exact (closed) type being resolved.</param>
	/// <param name="openServiceType">
	/// The open-generic type definition of <paramref name="serviceType"/>, or <see langword="null"/>
	/// when <paramref name="serviceType"/> is not generic.
	/// </param>
	/// <param name="service">The current (possibly already decorated) service instance.</param>
	/// <param name="sp">The owning <see cref="IServiceProvider"/>, forwarded to decorators that need it.</param>
	/// <returns>
	/// The decorated service, or <see langword="null"/> if this descriptor does not apply to
	/// <paramref name="serviceType"/>.
	/// </returns>
	public abstract object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp);

	/// <summary>
	/// Walks <paramref name="decoratorType"/>'s public constructors and returns the type of the first
	/// parameter whose type is an interface or base class that <paramref name="decoratorType"/> implements.
	/// Throws <see cref="InvalidOperationException"/> when no such constructor exists.
	/// </summary>
	internal static Type InferServiceTypeFromCtors(Type decoratorType)
	{
		foreach (var ctor in decoratorType.GetConstructors())
		{
			var first = ctor.GetParameters().FirstOrDefault();
			if (first is not null && first.ParameterType.IsAssignableFrom(decoratorType))
				return first.ParameterType;
		}

		throw Error.CannotInferServiceType(decoratorType);
	}

	/// <summary>
	/// Compiles a <c>Func&lt;object, IServiceProvider, object&gt;</c> that calls
	/// <c>new decoratorType(service, sp)</c> or <c>new decoratorType(service)</c>, preferring the
	/// two-parameter overload. The lambda is compiled once at registration time (fail-fast) and
	/// reused on every resolve — no <c>MethodBase.Invoke</c> or array allocation at call time.
	/// </summary>
	internal static Func<object, IServiceProvider, object> BuildCtorInvoker(Type serviceType, Type decoratorType)
	{
		var svcParam = Expression.Parameter(typeof(object), "svc");
		var spParam  = Expression.Parameter(typeof(IServiceProvider), "sp");
		var castSvc  = Expression.Convert(svcParam, serviceType);

		var ctor2 = decoratorType.GetConstructor([serviceType, typeof(IServiceProvider)]);
		if (ctor2 is not null)
		{
			var body = Expression.Convert(Expression.New(ctor2, castSvc, spParam), typeof(object));
			return Expression.Lambda<Func<object, IServiceProvider, object>>(body, svcParam, spParam).Compile();
		}

		var ctor1 = decoratorType.GetConstructor([serviceType]);
		if (ctor1 is not null)
		{
			var body = Expression.Convert(Expression.New(ctor1, castSvc), typeof(object));
			return Expression.Lambda<Func<object, IServiceProvider, object>>(body, svcParam, spParam).Compile();
		}

		throw Error.NoSuitableConstructor(serviceType, decoratorType);
	}
}

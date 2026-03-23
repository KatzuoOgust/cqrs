using System.Collections.Concurrent;
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
public abstract class Decorator
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

	// -----------------------------------------------------------------------
	// Factory methods — lambda-based
	// -----------------------------------------------------------------------

	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type
	/// is exactly <typeparamref name="TService"/>.
	/// </summary>
	public static Decorator Exact<TService>(Func<TService, IServiceProvider, TService> factory)
		where TService : class =>
		new ExactDecorator<TService>(factory);

	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type's
	/// open-generic definition matches <paramref name="openGenericServiceType"/>.
	/// The factory receives the closed service type, the inner instance, and the service provider.
	/// </summary>
	public static Decorator Generic(
		Type openGenericServiceType,
		Func<Type, object, IServiceProvider, object> factory) =>
		new GenericDecorator(openGenericServiceType, factory);

	// -----------------------------------------------------------------------
	// Factory methods — type-based (constructor resolved via Expression trees)
	// -----------------------------------------------------------------------

	/// <summary>
	/// Creates a descriptor that wraps <paramref name="serviceType"/> with <paramref name="decoratorType"/>.
	/// The constructor must accept <c>(serviceType)</c> or <c>(serviceType, IServiceProvider)</c>.
	/// Throws <see cref="InvalidOperationException"/> at registration time if no suitable constructor is found.
	/// </summary>
	public static Decorator ExactByType(Type serviceType, Type decoratorType)
	{
		var factory = BuildCtorInvoker(serviceType, decoratorType);
		return new ExactByTypeDecorator(serviceType, factory);
	}

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
		new GenericByTypeDecorator(openServiceType, openDecoratorType);

	/// <summary>
	/// Creates a descriptor that wraps <paramref name="inner"/> and only invokes it when
	/// <paramref name="predicate"/> returns <see langword="true"/> for the resolved service type.
	/// The predicate is evaluated before the inner descriptor, so the inner factory is never
	/// called for non-matching types.
	/// </summary>
	public static Decorator Conditional(Func<Type, bool> predicate, Decorator inner) =>
		new ConditionalDescriptor(predicate, inner);

	// -----------------------------------------------------------------------
	// Constructor resolution helpers
	// -----------------------------------------------------------------------

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

		throw new InvalidOperationException(
			$"Cannot infer service type for '{decoratorType}'. " +
			$"Ensure it has a constructor whose first parameter is an interface or base class that '{decoratorType}' implements. " +
			$"Alternatively use Decorate<TService, TDecorator>() to specify the service type explicitly.");
	}

	/// <summary>
	/// Compiles a <c>Func&lt;object, IServiceProvider, object&gt;</c> that calls
	/// <c>new decoratorType(service, sp)</c> or <c>new decoratorType(service)</c>, preferring the
	/// two-parameter overload. The lambda is compiled once at registration time (fail-fast) and
	/// reused on every resolve — no <c>MethodBase.Invoke</c> or array allocation at call time.
	/// </summary>
	private static Func<object, IServiceProvider, object> BuildCtorInvoker(Type serviceType, Type decoratorType)
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

		throw new InvalidOperationException(
			$"'{decoratorType}' has no constructor accepting '{serviceType}' " +
			$"(with or without a trailing IServiceProvider parameter).");
	}

	// -----------------------------------------------------------------------
	// Implementations
	// -----------------------------------------------------------------------

	private sealed class ExactDecorator<TService>(
		Func<TService, IServiceProvider, TService> factory) : Decorator
		where TService : class
	{
		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp)
			=> serviceType == typeof(TService) ? factory((TService)service, sp) : null;
	}

	private sealed class ExactByTypeDecorator(
		Type serviceType,
		Func<object, IServiceProvider, object> factory) : Decorator
	{
		public override object? TryApply(Type st, Type? openServiceType, object service, IServiceProvider sp)
			=> st == serviceType ? factory(service, sp) : null;
	}

	private sealed class GenericDecorator(
		Type openGenericServiceType,
		Func<Type, object, IServiceProvider, object> factory) : Decorator
	{
		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp)
			=> openServiceType == openGenericServiceType ? factory(serviceType, service, sp) : null;
	}

	private sealed class GenericByTypeDecorator(
		Type openServiceType,
		Type openDecoratorType) : Decorator
	{
		private readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, object>> _cache = new();

		public override object? TryApply(Type serviceType, Type? openType, object service, IServiceProvider sp)
		{
			if (openType != openServiceType) return null;

			var factory = _cache.GetOrAdd(serviceType, BuildFactory);
			return factory(service, sp);
		}

		private Func<object, IServiceProvider, object> BuildFactory(Type closedServiceType)
		{
			var closedDecorator = openDecoratorType.MakeGenericType(closedServiceType.GetGenericArguments());
			return BuildCtorInvoker(closedServiceType, closedDecorator);
		}
	}

	private sealed class ConditionalDescriptor(
		Func<Type, bool> predicate,
		Decorator inner) : Decorator
	{
		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp) =>
			predicate(serviceType) ? inner.TryApply(serviceType, openServiceType, service, sp) : null;
	}
}

using System.Linq.Expressions;
using System.Reflection;

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
	/// Compiles a <c>Func&lt;object, IServiceProvider, object&gt;</c> that instantiates
	/// <paramref name="decoratorType"/> by injecting the inner service and resolving any
	/// additional constructor parameters from <see cref="IServiceProvider"/>. Three constructor
	/// forms are tried in order:
	/// <list type="number">
	///   <item><c>new decoratorType(service, IServiceProvider)</c></item>
	///   <item><c>new decoratorType(service)</c></item>
	///   <item><c>new decoratorType(service, p2, p3, …)</c> — extra params resolved via <c>sp.GetService</c></item>
	/// </list>
	/// The lambda is compiled once at registration time (fail-fast) and reused on every resolve —
	/// no <c>MethodBase.Invoke</c> or array allocation at call time.
	/// </summary>
	internal static Func<object, IServiceProvider, object> BuildCtorInvoker(Type serviceType, Type decoratorType)
	{
		// Strategy 1: ctor(serviceType, IServiceProvider)
		var ctor2 = decoratorType.GetConstructor([serviceType, typeof(IServiceProvider)]);
		if (ctor2 is not null)
		{
			return CompileStrategy1(serviceType, ctor2);
		}

		// Strategy 2: ctor(serviceType)
		var ctor1 = decoratorType.GetConstructor([serviceType]);
		if (ctor1 is not null)
		{
			return CompileStrategy2(serviceType, ctor1);
		}

		// Strategy 3: ctor(serviceType, p2, p3, …) — extra params resolved from sp at call time
		foreach (var ctor in decoratorType.GetConstructors())
		{
			var parameters = ctor.GetParameters();
			if (parameters.Length < 2) continue;
			if (parameters[0].ParameterType.IsAssignableFrom(serviceType))
			{
				return CompileStrategy3(serviceType, ctor, parameters);
			}
		}

		throw Error.NoSuitableConstructor(serviceType, decoratorType);

		static Func<object, IServiceProvider, object> CompileStrategy1(Type serviceType, ConstructorInfo ctor2)
		{
			var svcParam = Expression.Parameter(typeof(object), "svc");
			var spParam  = Expression.Parameter(typeof(IServiceProvider), "sp");
			var castSvc  = Expression.Convert(svcParam, serviceType);

			var body = Expression.Convert(Expression.New(ctor2, castSvc, spParam), typeof(object));
			return Expression.Lambda<Func<object, IServiceProvider, object>>(body, svcParam, spParam).Compile();
		}

		static Func<object, IServiceProvider, object> CompileStrategy2(Type serviceType, ConstructorInfo ctor1)
		{
			var svcParam = Expression.Parameter(typeof(object), "svc");
			var spParam  = Expression.Parameter(typeof(IServiceProvider), "sp");
			var castSvc  = Expression.Convert(svcParam, serviceType);

			var body = Expression.Convert(Expression.New(ctor1, castSvc), typeof(object));
			return Expression.Lambda<Func<object, IServiceProvider, object>>(body, svcParam, spParam).Compile();
		}

		static Func<object, IServiceProvider, object> CompileStrategy3(Type serviceType, ConstructorInfo ctor, ParameterInfo[] parameters)
		{
			var svcParam = Expression.Parameter(typeof(object), "svc");
			var spParam  = Expression.Parameter(typeof(IServiceProvider), "sp");
			var castSvc  = Expression.Convert(svcParam, serviceType);

			var getService = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;

			var args = new Expression[parameters.Length];
			args[0] = castSvc;
			for (var i = 1; i < parameters.Length; i++)
			{
				args[i] = Expression.Convert(
					Expression.Call(spParam, getService, Expression.Constant(parameters[i].ParameterType)),
					parameters[i].ParameterType
				);
			}

			var body = Expression.Convert(Expression.New(ctor, args), typeof(object));
			return Expression.Lambda<Func<object, IServiceProvider, object>>(body, svcParam, spParam).Compile();
		}
	}
}

using System.Runtime.CompilerServices;

namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

public abstract partial class Decorator
{
	private static class Error
	{
		internal static void ThrowIfNotOpenGenericTypeDefinition(
			Type type,
			[CallerArgumentExpression(nameof(type))] string paramName = "")
		{
			if (!type.IsGenericTypeDefinition)
				throw new ArgumentException(
					$"'{type}' is not an open generic type definition.",
					paramName);
		}

		internal static InvalidOperationException NoSuitableConstructor(Type serviceType, Type decoratorType) =>
			new($"'{decoratorType}' has no suitable constructor for service type '{serviceType}'. " +
				$"Accepted forms: ctor({serviceType}), ctor({serviceType}, IServiceProvider), " +
				$"or ctor({serviceType}, p2, p3, …) where additional parameters are resolved from IServiceProvider.");

		internal static InvalidOperationException CannotInferServiceType(Type decoratorType) =>
			new($"Cannot infer service type for '{decoratorType}'. " +
				$"Ensure it has a constructor whose first parameter is an interface or base class that '{decoratorType}' implements. " +
				$"Alternatively use Decorate<TService, TDecorator>() to specify the service type explicitly.");
	}
}

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
			new($"'{decoratorType}' has no constructor accepting '{serviceType}' " +
				$"(with or without a trailing IServiceProvider parameter).");

		internal static InvalidOperationException CannotInferServiceType(Type decoratorType) =>
			new($"Cannot infer service type for '{decoratorType}'. " +
				$"Ensure it has a constructor whose first parameter is an interface or base class that '{decoratorType}' implements. " +
				$"Alternatively use Decorate<TService, TDecorator>() to specify the service type explicitly.");
	}
}

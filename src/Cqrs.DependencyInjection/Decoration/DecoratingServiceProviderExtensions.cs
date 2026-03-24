using System.Runtime.CompilerServices;

namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

/// <summary>Extension methods for registering decorators on a <see cref="DecoratingServiceProvider"/>.</summary>
public static class DecoratingServiceProviderExtensions
{
	extension(DecoratingServiceProvider provider)
	{
		/// <summary>Registers an exact-type decorator for <typeparamref name="TService"/>.</summary>
		public DecoratingServiceProvider Decorate<TService>(Func<TService, IServiceProvider, TService> decorator)
			where TService : class
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentNullException.ThrowIfNull(decorator);

			provider.Add(Decorator.Exact(decorator));
			return provider;
		}

		/// <summary>
		/// Registers an exact-type decorator, inferring the service type from
		/// <typeparamref name="TDecorator"/>'s first constructor parameter.
		/// The constructor must accept <c>(TService)</c> or <c>(TService, IServiceProvider)</c>.
		/// </summary>
		public DecoratingServiceProvider Decorate<TDecorator>()
			where TDecorator : class
		{
			ArgumentNullException.ThrowIfNull(provider);

			var decoratorType = typeof(TDecorator);
			provider.Add(Decorator.Exact(Decorator.InferServiceType(decoratorType), decoratorType));
			return provider;
		}

		/// <summary>
		/// Registers an exact-type decorator for <typeparamref name="TService"/> using
		/// <typeparamref name="TDecorator"/>. The constructor must accept
		/// <c>(TService)</c> or <c>(TService, IServiceProvider)</c>.
		/// </summary>
		public DecoratingServiceProvider Decorate<TService, TDecorator>()
			where TService : class
			where TDecorator : class, TService
		{
			ArgumentNullException.ThrowIfNull(provider);

			provider.Add(Decorator.Exact(typeof(TService), typeof(TDecorator)));
			return provider;
		}

		/// <summary>
		/// Registers an open-generic decorator. <paramref name="openGenericServiceType"/> must be an open
		/// generic type definition (e.g. <c>typeof(ICommandHandler&lt;&gt;)</c>).
		/// The factory receives the closed service type, the resolved inner instance, and this provider.
		/// </summary>
		public DecoratingServiceProvider Decorate(Type openGenericServiceType,
			Func<Type, object, IServiceProvider, object> decorator)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentNullException.ThrowIfNull(openGenericServiceType);
			ArgumentNullException.ThrowIfNull(decorator);
			Error.ThrowIfNotOpenGenericTypeDefinition(openGenericServiceType);

			provider.Add(Decorator.Generic(openGenericServiceType, decorator));
			return provider;
		}

		/// <summary>
		/// Registers an open-generic decorator. <paramref name="openGenericServiceType"/> must be an open
		/// generic type definition (e.g. <c>typeof(ICommandHandler&lt;&gt;)</c>).
		/// The factory receives the closed service type and the resolved inner instance.
		/// </summary>
		public DecoratingServiceProvider Decorate(Type openGenericServiceType,
			Func<Type, object, object> decorator)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentNullException.ThrowIfNull(openGenericServiceType);
			ArgumentNullException.ThrowIfNull(decorator);
			Error.ThrowIfNotOpenGenericTypeDefinition(openGenericServiceType);

			provider.Add(Decorator.Generic(openGenericServiceType, decorator));
			return provider;
		}

		/// <summary>
		/// Registers an open-generic decorator using <paramref name="openDecoratorType"/>, auto-building
		/// the closed decorator via constructor reflection — no factory lambda needed.
		/// Both arguments must be open generic type definitions.
		/// The decorator's constructor must accept <c>(TService)</c> or <c>(TService, IServiceProvider)</c>.
		/// </summary>
		public DecoratingServiceProvider Decorate(Type openGenericServiceType,
			Type openDecoratorType)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentNullException.ThrowIfNull(openGenericServiceType);
			ArgumentNullException.ThrowIfNull(openDecoratorType);
			Error.ThrowIfNotOpenGenericTypeDefinition(openGenericServiceType);
			Error.ThrowIfNotOpenGenericTypeDefinition(openDecoratorType);

			provider.Add(Decorator.Generic(openGenericServiceType, openDecoratorType));
			return provider;
		}

		/// <summary>
		/// Applies all decorators registered in <paramref name="configure"/> only when
		/// <paramref name="predicate"/> returns <see langword="true"/> for the resolved service type.
		/// </summary>
		public DecoratingServiceProvider When(Func<Type, bool> predicate,
			Func<DecoratingServiceProvider, DecoratingServiceProvider> configure)
		{
			ArgumentNullException.ThrowIfNull(provider);
			ArgumentNullException.ThrowIfNull(predicate);
			ArgumentNullException.ThrowIfNull(configure);

			var collector = new DecoratingServiceProvider(NullServiceProvider.Instance);
			configure(collector);

			foreach (var descriptor in collector.Decorators)
				provider.Add(Decorator.Conditional(predicate, descriptor));

			return provider;
		}
	}

	private sealed class NullServiceProvider : IServiceProvider
	{
		public static readonly NullServiceProvider Instance = new();
		public object? GetService(Type serviceType) => null;
	}

	private static class Error
	{
		internal static void ThrowIfNotOpenGenericTypeDefinition(
			Type type,
			[CallerArgumentExpression(nameof(type))] string paramName = "")
		{
			if (type.IsGenericTypeDefinition) return;

			throw new ArgumentException(
				$"'{type}' is not an open generic type definition.",
				paramName);
		}
	}
}

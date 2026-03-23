namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

public abstract partial class Decorator
{
	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type
	/// is exactly <typeparamref name="TService"/>.
	/// </summary>
	public static Decorator Exact<TService>(Func<TService, IServiceProvider, TService> factory)
		where TService : class =>
		new ExactDecorator<TService>(factory);

	/// <summary>
	/// Creates a descriptor that applies <paramref name="factory"/> whenever the resolved type
	/// is exactly <typeparamref name="TService"/>.
	/// </summary>
	public static Decorator Exact<TService>(Func<TService, TService> factory)
		where TService : class =>
		new ExactDecorator<TService>(factory);

	/// <summary>
	/// Creates a descriptor that wraps <paramref name="serviceType"/> with <paramref name="decoratorType"/>.
	/// The constructor must accept <c>(serviceType)</c> or <c>(serviceType, IServiceProvider)</c>.
	/// Throws <see cref="InvalidOperationException"/> at registration time if no suitable constructor is found.
	/// </summary>
	public static Decorator ExactByType(Type serviceType, Type decoratorType) =>
		new ExactDecorator(serviceType, BuildCtorInvoker(serviceType, decoratorType));

	private sealed class ExactDecorator(
		Type serviceType,
		Func<object, IServiceProvider, object> factory) : Decorator
	{
		public override object? TryApply(Type st, Type? openServiceType, object service, IServiceProvider sp)
			=> st == serviceType ? factory(service, sp) : null;
	}

	private sealed class ExactDecorator<TService> : Decorator
		where TService : class
	{
		private readonly Func<TService, IServiceProvider, TService> _factory;

		public ExactDecorator(Func<TService, IServiceProvider, TService> factory)
			=> _factory = factory;

		public ExactDecorator(Func<TService, TService> factory)
			: this((svc, _) => factory(svc)) { }

		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp)
			=> serviceType == typeof(TService) ? _factory((TService)service, sp) : null;
	}
}

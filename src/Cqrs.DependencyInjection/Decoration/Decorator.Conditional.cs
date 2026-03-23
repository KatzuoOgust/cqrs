namespace KatzuoOgust.Cqrs.DependencyInjection.Decoration;

public abstract partial class Decorator
{
	/// <summary>
	/// Creates a descriptor that wraps <paramref name="inner"/> and only invokes it when
	/// <paramref name="predicate"/> returns <see langword="true"/> for the resolved service type.
	/// The predicate is evaluated before the inner descriptor, so the inner factory is never
	/// called for non-matching types.
	/// </summary>
	public static Decorator Conditional(Func<Type, bool> predicate, Decorator inner) =>
		new ConditionalDescriptor(predicate, inner);

	private sealed class ConditionalDescriptor(
		Func<Type, bool> predicate,
		Decorator inner) : Decorator
	{
		public override object? TryApply(Type serviceType, Type? openServiceType, object service, IServiceProvider sp) =>
			predicate(serviceType) ? inner.TryApply(serviceType, openServiceType, service, sp) : null;
	}
}

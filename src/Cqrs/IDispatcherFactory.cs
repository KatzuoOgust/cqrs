namespace KatzuoOgust.Cqrs;

/// <summary>Creates <see cref="IDispatcher"/> instances.</summary>
public interface IDispatcherFactory
{
	/// <summary>Creates and returns a new <see cref="IDispatcher"/>.</summary>
	public IDispatcher Create();
}

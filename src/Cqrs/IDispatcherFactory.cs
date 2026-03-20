namespace KatzuoOgust.Cqrs;

/// <summary>Creates <see cref="IDispatcher"/> instances.</summary>
public interface IDispatcherFactory
{
	IDispatcher Create();
}

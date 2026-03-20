namespace KatzuoOgust.Cqrs;

/// <summary>Creates <see cref="IEventBus"/> instances.</summary>
public interface IEventBusFactory
{
	IEventBus Create();
}

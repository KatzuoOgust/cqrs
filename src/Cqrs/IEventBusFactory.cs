namespace KatzuoOgust.Cqrs;

/// <summary>Creates <see cref="IEventBus"/> instances.</summary>
public interface IEventBusFactory
{
	public IEventBus Create();
}

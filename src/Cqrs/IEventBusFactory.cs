namespace KatzuoOgust.Cqrs;

/// <summary>Creates <see cref="IEventBus"/> instances.</summary>
public interface IEventBusFactory
{
	/// <summary>Creates and returns a new <see cref="IEventBus"/>.</summary>
	public IEventBus Create();
}

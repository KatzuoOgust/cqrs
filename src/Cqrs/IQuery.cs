namespace KatzuoOgust.Cqrs;

/// <summary>Marker interface for a query that produces <typeparamref name="TResponse"/>.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

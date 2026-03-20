namespace KatzuoOgust.Cqrs;

/// <summary>Marker interface for all requests.</summary>
public interface IRequest { }

/// <summary>Marker interface for a request that produces <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse> : IRequest { }

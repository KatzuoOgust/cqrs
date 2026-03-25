namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

/// <summary>Represents the next step in an event behaviour pipeline.</summary>
public delegate Task EventBehaviourDelegate(CancellationToken ct);

/// <summary>Represents the next step in a request behaviour pipeline.</summary>
public delegate Task<object?> RequestBehaviourDelegate(CancellationToken ct);

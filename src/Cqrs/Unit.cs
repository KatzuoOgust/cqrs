namespace KatzuoOgust.Cqrs;

/// <summary>Represents a void return type for commands that produce no result.</summary>
public readonly struct Unit : IEquatable<Unit>
{
	/// <summary>The singleton value of <see cref="Unit"/>.</summary>
	public static readonly Unit Value = new();

	/// <inheritdoc/>
	public bool Equals(Unit other) => true;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is Unit;
	/// <inheritdoc/>
	public override int GetHashCode() => 0;
	/// <inheritdoc/>
	public override string ToString() => "()";

	/// <summary>Returns <see langword="true"/>; all <see cref="Unit"/> values are equal.</summary>
	public static bool operator ==(Unit left, Unit right) => true;
	/// <summary>Returns <see langword="false"/>; all <see cref="Unit"/> values are equal.</summary>
	public static bool operator !=(Unit left, Unit right) => false;
}

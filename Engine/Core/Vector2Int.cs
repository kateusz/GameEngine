namespace Engine.Core;

/// <summary>
/// Represents a 2D vector with integer components.
/// </summary>
public struct Vector2Int
{
    /// <summary>
    /// X component of the vector.
    /// </summary>
    public int X;

    /// <summary>
    /// Y component of the vector.
    /// </summary>
    public int Y;

    /// <summary>
    /// Creates a new Vector2Int with the specified components.
    /// </summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    public Vector2Int(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Returns a string representation of this vector.
    /// </summary>
    public override string ToString() => $"({X}, {Y})";

    /// <summary>
    /// Checks equality with another Vector2Int.
    /// </summary>
    public override bool Equals(object? obj) => obj is Vector2Int other && X == other.X && Y == other.Y;

    /// <summary>
    /// Gets the hash code for this vector.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Vector2Int left, Vector2Int right) => !left.Equals(right);
}

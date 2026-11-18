using System;

namespace Supermarket.Building
{
  /// <summary>
  /// An integer cell coordinate on the build grid. X is right, Z is forward.
  /// </summary>
  public readonly struct GridCoord : IEquatable<GridCoord>
  {
    public readonly int X;
    public readonly int Z;

    public GridCoord(int x, int z)
    {
      X = x;
      Z = z;
    }

    public static GridCoord Zero
    {
      get { return new GridCoord(0, 0); }
    }

    public static GridCoord operator +(GridCoord a, GridCoord b)
    {
      return new GridCoord(a.X + b.X, a.Z + b.Z);
    }

    public static GridCoord operator -(GridCoord a, GridCoord b)
    {
      return new GridCoord(a.X - b.X, a.Z - b.Z);
    }

    public static bool operator ==(GridCoord a, GridCoord b)
    {
      return a.X == b.X && a.Z == b.Z;
    }

    public static bool operator !=(GridCoord a, GridCoord b)
    {
      return !(a == b);
    }

    public bool Equals(GridCoord other)
    {
      return X == other.X && Z == other.Z;
    }

    public override bool Equals(object obj)
    {
      return obj is GridCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return (X * 397) ^ Z;
      }
    }

    public override string ToString()
    {
      return string.Format("({0}, {1})", X, Z);
    }
  }
}

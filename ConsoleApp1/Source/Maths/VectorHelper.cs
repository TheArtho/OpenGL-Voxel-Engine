using System.Numerics;

namespace Minecraft;

public static class VectorHelper
{
    public static bool IsValidVector(Vector3 vector)
    {
        return !float.IsNaN(vector.X) && !float.IsNaN(vector.Y) && !float.IsNaN(vector.Z) &&
               !float.IsInfinity(vector.X) && !float.IsInfinity(vector.Y) && !float.IsInfinity(vector.Z);
    }
}
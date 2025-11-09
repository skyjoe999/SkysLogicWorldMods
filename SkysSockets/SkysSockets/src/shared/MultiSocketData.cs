using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkysSockets.Shared;

public static class MultiSocket
{
    // Because apperently you cant convert to Quaternions server side!
    private const float sqrt2 = 1.41421356237f;

    public static int MultiSocketMaxInputs = 16;
    public const int MultiSocketDefaultInputs = 3;

    public static List<Vector3> GetSocketPositions(int InputCount)
        => (from i in Enumerable.Range(0, InputCount)
            select new Vector3(i / 3f, 1f / 3f, 0.5f)).ToList();

    public static List<Quaternion> GetSocketRotations(int InputCount)
        => Enumerable.Repeat(new Quaternion(1 / sqrt2, 0, 0, 1 / sqrt2), InputCount).ToList();
}

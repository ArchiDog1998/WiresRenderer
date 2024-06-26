using Grasshopper.Kernel;
using HarmonyLib;
using System;
using System.Drawing;

namespace WiresRenderer.Patch;

[HarmonyPatch(typeof(GH_Document))]
internal class DocumentPatch
{
    [HarmonyPatch("DistanceToWire")]
    static bool Prefix(ref float __result, PointF locus, float radius, PointF source, PointF target)
    {
        if (locus.Y < source.Y - radius && locus.Y < target.Y - radius)
        {
            __result = float.MaxValue;
            return false;
        }
        if (locus.Y > source.Y + radius && locus.Y > target.Y + radius)
        {
            __result = float.MaxValue;
            return false;
        }

        var path = PainterPatch.GetPath(source, target);
        path.Flatten();
        PointF[] points = path.PathPoints;
        float min = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
        {
            min = Math.Min(min, PointToLine(points[i], points[i + 1], locus));
        }
        __result = min;
        return false;
    }

    private static float PointToLine(PointF p1, PointF p2, PointF p)
    {
        float a, b, c;
        a = Distance(p1, p2);
        b = Distance(p1, p);
        c = Distance(p2, p);
        if (c + b == a)
        {
            return 0;
        }
        if (a <= 0.00001)
        {
            return b;
        }
        if (c * c >= a * a + b * b)
        {
            return b;
        }
        if (b * b >= a * a + c * c)
        {
            return c;
        }

        float p0 = (a + b + c) / 2;
        float s = (float)Math.Sqrt(p0 * (p0 - a) * (p0 - b) * (p0 - c));
        return 2 * s / a;
    }

    private static float Distance(PointF p, PointF p1)
        => (float)Math.Sqrt(Math.Pow(p.X - p1.X, 2) + Math.Pow(p.Y - p1.Y, 2));
}

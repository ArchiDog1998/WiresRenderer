using Grasshopper.GUI.Canvas;
using HarmonyLib;
using System.Drawing;

namespace WiresRenderer;

[HarmonyPatch(typeof(GH_Painter), "GenerateWirePen")]

internal class GeneratePenPatch
{
    static void Postfix(ref Pen __result)
    {
        __result.Width *= Data.WireWidth;
    }
}

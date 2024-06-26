using Grasshopper.GUI.Canvas;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace WiresRenderer.Patch;

[HarmonyPatch(typeof(GH_Canvas))]
internal class CanvasPatch
{
    [HarmonyPatch("InsertRelayIntoWire")]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var list = new List<CodeInstruction>();
        bool find = false;
        foreach (var instruction in instructions)
        {
            if (find)
            {
                list.Add(instruction);
            }
            else if (instruction.opcode == OpCodes.Starg_S)
            {
                find = true;
            }
        }
        return list;
    }
}

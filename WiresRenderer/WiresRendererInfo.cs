using Grasshopper.GUI;
using Grasshopper.Kernel;
using HarmonyLib;
using SimpleGrasshopper.Util;
using System;
using System.Drawing;

namespace WiresRenderer;

public class WiresRendererInfo : GH_AssemblyInfo
{
    public override string Name => "Wires Renderer";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon =>  typeof(WiresRendererInfo).Assembly.GetBitmap("WiresRendererIcons_24.png")!;

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "Provide a set of wire types such as Polyline. Also it can change wire's color and width.";

    public override Guid Id => new ("2850F61C-B64C-4466-8B9C-D89754AB6ECD");

    //Return a string identifying you or your company.
    public override string AuthorName => "秋水";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "1123993881@qq.com";

    public override string Version => "1.4.2";
}

partial class SimpleAssemblyPriority : IDisposable
{
    private Harmony? _harmony;

    protected override void DoWithEditor(GH_DocumentEditor editor)
    {
        base.DoWithEditor(editor);

        _harmony = new Harmony("grasshopper.WiresRenderer");
        _harmony.PatchAll();
    }

    public void Dispose()
    {
        _harmony?.UnpatchAll();
        GC.SuppressFinalize(this);
    }
}
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;
using SimpleGrasshopper.Util;
using Grasshopper.GUI.Canvas.TagArtists;
using Grasshopper.Kernel.Special;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace WiresRenderer;

public class WiresRendererInfo : GH_AssemblyInfo
{
    public override string Name => "Wires Renderer";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon =>  typeof(WiresRendererInfo).Assembly.GetBitmap("WiresRendererIcons_24.png");

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "Provide a set of wire types such as Polyline. Also it can change wire's color and width.";

    public override Guid Id => new ("2850F61C-B64C-4466-8B9C-D89754AB6ECD");

    //Return a string identifying you or your company.
    public override string AuthorName => "秋水";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "1123993881@qq.com";

    public override string Version => "1.3.9";
}

partial class SimpleAssemblyPriority : IDisposable
{
    private static readonly FieldInfo _artTags = typeof(GH_Canvas).GetRuntimeFields().First(m => m.Name.Contains("_artists"));

    protected override void DoWithEditor(GH_DocumentEditor editor)
    {
        base.DoWithEditor(editor);

        var canvas = Instances.ActiveCanvas;

        canvas.Document_ObjectsAdded += ActiveCanvas_Document_ObjectsAdded;
        canvas.CanvasPaintBegin += ActiveCanvas_CanvasPaintBegin;

        canvas.CanvasPrePaintWires += Canvas_CanvasPrePaintWires;
        canvas.CanvasPostPaintWires += Canvas_CanvasPostPaintWires;
    }

    public void Dispose()
    {
        var canvas = Instances.ActiveCanvas;

        canvas.Document_ObjectsAdded -= ActiveCanvas_Document_ObjectsAdded;
        canvas.CanvasPaintBegin -= ActiveCanvas_CanvasPaintBegin;

        canvas.CanvasPrePaintWires -= Canvas_CanvasPrePaintWires;
        canvas.CanvasPostPaintWires -= Canvas_CanvasPostPaintWires;

        GC.SuppressFinalize(this);
    }

    private Color _default, _empty, _selectA, _selectB;
    private void Canvas_CanvasPostPaintWires(GH_Canvas sender)
    {
        GH_Skin.wire_default = _default;
        GH_Skin.wire_empty = _empty;
        GH_Skin.wire_selected_a = _selectA;
        GH_Skin.wire_selected_b = _selectB;
    }

    private void Canvas_CanvasPrePaintWires(GH_Canvas sender)
    {
        var painter = sender.Painter;
        foreach (var obj in sender.Document.Objects)
        {
            if(obj is IGH_Component comp)
            {
                foreach (var param in comp.Params.Input)
                {
                    RenderAllInputs(painter, param);
                }
            }
            if (obj is IGH_Param p)
            {
                RenderAllInputs(painter, p);
            }
        }

        _default = GH_Skin.wire_default;
        _empty = GH_Skin.wire_empty;
        _selectA = GH_Skin.wire_selected_a;
        _selectB = GH_Skin.wire_selected_b;

        GH_Skin.wire_default = GH_Skin.wire_empty = GH_Skin.wire_selected_a = GH_Skin.wire_selected_b = Color.Transparent;
    }

    private static void RenderAllInputs(GH_Painter painter, IGH_Param param)
    {
        foreach(var input in param.Sources)
        {
            WireDrawer.DrawConnection(painter, input, param);
        }
    }

    private static void ActiveCanvas_CanvasPaintBegin(GH_Canvas sender)
    {
        List<IGH_TagArtist> tags = (List<IGH_TagArtist>)_artTags.GetValue(sender);
        if (tags == null || tags.Count == 0) return;

        for (int i = 0; i < tags.Count; i++)
        {
            if (tags[i] is GH_TagArtist_WirePainter and not NewTag)
            {
                tags[i] = new NewTag((GH_TagArtist_WirePainter)tags[i]);
            }
        }
        _artTags.SetValue(sender, tags);
    }

    private static void ActiveCanvas_Document_ObjectsAdded(GH_Document sender, GH_DocObjectEventArgs e)
    {
        if (e.ObjectCount != 1) return;
        IGH_DocumentObject obj = e.Objects[0];
        if (obj is not GH_Relay delay || delay.SourceCount == 0) return;

        Point pointOnCanvas = Instances.ActiveCanvas.PointToClient(Control.MousePosition);
        PointF pointOnDocument = Instances.ActiveCanvas.Viewport.UnprojectPoint(pointOnCanvas);
        obj.Attributes.Pivot = pointOnDocument;
    }

}
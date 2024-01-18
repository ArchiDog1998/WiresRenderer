using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Canvas.TagArtists;
using Grasshopper.Kernel;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace WiresRenderer;

public class NewTag(IGH_Param source, IGH_Param target, Color colour, int width) 
    : GH_TagArtist_WirePainter(source, target, colour, width)
{
    private static readonly FieldInfo _colourInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().First(m => m.Name.Contains("m_colour"));
    private static readonly FieldInfo _widthInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().First(m => m.Name.Contains("m_width"));
    private static readonly FieldInfo _sourceInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().First(m => m.Name.Contains("m_source"));
    private static readonly FieldInfo _targetInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().First(m => m.Name.Contains("m_target"));

    public NewTag(GH_TagArtist_WirePainter other)
        :this(null, null, Color.DarkRed, 5)
    {
        _sourceInfo.SetValue(this, _sourceInfo.GetValue(other));
        _targetInfo.SetValue(this, _targetInfo.GetValue(other));
    }

    public override void Paint(GH_Canvas canvas, GH_CanvasChannel channel)
    {
        if (channel != GH_CanvasChannel.Wires) return;

        using var pen = new Pen((Color)_colourInfo.GetValue(this), (int)_widthInfo.GetValue(this));
        using var graphicsPath = WireDrawer.GetPath((IGH_Param)_sourceInfo.GetValue(this),
            (IGH_Param)_targetInfo.GetValue(this));

        try
        {
            canvas.Graphics.DrawPath(pen, graphicsPath);
        }
        catch
        {

        }
    }
}

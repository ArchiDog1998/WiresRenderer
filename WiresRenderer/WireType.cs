using SimpleGrasshopper.Attributes;
using System.ComponentModel;

namespace WiresRenderer;

public enum WireType
{
    [Icon("BezierWireIcons_24.png")]
    [Description("Bezier Ratio Wire")]
    RatioBezierWire,

    [Icon("BezierWireIcons_24.png")]
    [Description("Bezier Length Wire")]
    LengthBezierWire,

    [Icon("LineWireIcons_24.png")]
    [Description("Line Wire")]
    LineWire,

    [Icon("PolylineWireIcons_24.png")]
    [Description("Polyline Wire")]
    PolylineWire,

    [Icon("ElectricWireIcons_24.png")]
    [Description("Electric Wire")]
    ElectricWire,
}

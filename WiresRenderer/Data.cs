using Grasshopper.GUI.Canvas;
using SimpleGrasshopper.Attributes;
using System.Drawing;

namespace WiresRenderer;

internal static partial class Data
{
    [Setting, Config("Wire Types")]
    private static readonly WireType _wiretype = WireType.ElectricWire;

    [Range(0, 1, 3)]
    [Setting, Config("Bezier X Ratio", parent: "Wire Types." + nameof(WireType.RatioBezierWire))]
    private static readonly float _bezierRatioX = 0.5f;

    [Range(0, 1, 3)]
    [Setting, Config("Bezier Y Ratio", parent: "Wire Types." + nameof(WireType.RatioBezierWire))]
    private static readonly float _bezierRatioY = 0.75f;

    [Range(0, 500)]
    [Setting, Config("Bezier Length", parent: "Wire Types." + nameof(WireType.LengthBezierWire))]
    private static readonly float _bezierLength = 100;

    [Range(0, 100)]
    [Setting, Config("End Extend Length", parent: "Wire Types." + nameof(WireType.LineWire))]
    private static readonly float _lineExtend = 3;

    [Range(0, 100)]
    [Setting, Config("Corner Radius", parent: "Wire Types." + nameof(WireType.LineWire))]
    private static readonly float _lineRadius = 8;

    [Range(0, 100)]
    [Setting, Config("End Extend Length", parent: "Wire Types." + nameof(WireType.PolylineWire))]
    private static readonly float _polylineExtend = 3;

    [Range(0, 1, 3)]
    [Setting, Config("Multiple of Corner Pitch", parent: "Wire Types." + nameof(WireType.PolylineWire))]
    private static readonly float _polylineMulty = 0.3f;

    [Range(0, 100)]
    [Setting, Config("Corner Radius", parent: "Wire Types." + nameof(WireType.PolylineWire))]
    private static readonly float _polylineRadius = 12;

    [Range(0, 100)]
    [Setting, Config("End Extend Length", parent: "Wire Types." + nameof(WireType.ElectricWire))]
    private static readonly float _electricExtend = 5;

    [Range(0, 1, 3)]
    [Setting, Config("Multiple of Corner Pitch", parent: "Wire Types." + nameof(WireType.ElectricWire))]
    private static readonly float _electricMulty = 1;

    [Range(0, 1000)]
    [Setting, Config("Corner Radius 1", parent: "Wire Types." + nameof(WireType.ElectricWire))]
    private static readonly float _electricRadius1 = 100;

    [Range(0, 100)]
    [Setting, Config("Corner Radius 2", parent: "Wire Types." + nameof(WireType.ElectricWire))]
    private static readonly float _electricRadius2 = 12;

    [Config("Default Color", section: 1)]
    public static Color WireDefault 
    { 
        get => GH_Skin.wire_default;
        set => GH_Skin.wire_default = value;
    }

    [Config("Empty Color", section: 1)]
    public static Color WireEmpty
    {
        get => GH_Skin.wire_empty;
        set => GH_Skin.wire_empty = value;
    }

    [Config("Selected A Color", section: 1)]
    public static Color WireSelectedA
    {
        get => GH_Skin.wire_selected_a;
        set => GH_Skin.wire_selected_a = value;
    }

    [ Config("Selected B Color", section: 1)]
    public static Color WireSelectedB
    {
        get => GH_Skin.wire_selected_b;
        set => GH_Skin.wire_selected_b = value;
    }

    [Range(0.001, 10, 3)]
    [Setting, Config("Wire Width", section: 2)]
    private static readonly float _wireWidth = 1;
}

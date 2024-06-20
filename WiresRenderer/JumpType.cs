using System.ComponentModel;

namespace WiresRenderer;
internal enum JumpType
{
    [Description("Whole Screen")]
    WholeScreen,

    [Description("Two Objects")]
    TwoObjects,

    [Description("Direct Move")]
    DirectMove,
}

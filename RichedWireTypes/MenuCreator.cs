using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RichedWireTypes
{
    public static class MenuCreator
    {
        internal static readonly string _wiretype = "WireType";
        internal static readonly WireTypes _wiretypeDefault = WireTypes.Bezier;

        internal static readonly string _lineExtend = "LineExtend";
        internal static readonly double _lineExtendDefault = 2.5;

        internal static readonly string _lineRadius = "LineRadius";
        internal static readonly double _lineRadiusDefault = 5;

        internal static readonly string _polylineExtend = "PolylineExtend";
        internal static readonly double _polylineExtendDefault = 2.5;

        internal static readonly string _polylineMulty = "PolylineMulty";
        internal static readonly double _polylineMultyDefault = 0.3;

        internal static readonly string _polylineRadius = "PolylineRadius";
        internal static readonly double _polylineRadiusDefault = 5;

        internal static readonly string _wireWidth = "WireWidth";
        internal static readonly double _wireWidthDefault = 1;

        public static ToolStripMenuItem CreateMajorMenu()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Riched Wire Types", Properties.Resources.RichedWireTypesIcons_24, new ToolStripItem[]
            {
                CreateWireType()
            })
            { ToolTipText = "Change wire type or change wire width."};
            CreateNumberBox(major, "Multiple of Wire Width", _wireWidth, _wireWidthDefault, 10, 0.001);

            return major;
        }

        private static ToolStripMenuItem CreateWireType()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Wire Types") { ToolTipText = "Provide a list of wire types" };
            major.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateBezierMenu(),CreateLineMenu(), CreatePolylineMenu(),
            });

            return major;
        }

        private static ToolStripMenuItem CreateBezierMenu()
        {
            bool isOn = Grasshopper.Instances.Settings.GetValue(_wiretype, (int)_wiretypeDefault) == (int)WireTypes.Bezier;
            ToolStripMenuItem major = new ToolStripMenuItem("Bezier Wire", Properties.Resources.BezierWireIcons_24)
            {
                Tag = WireTypes.Bezier,
                Checked = isOn,
            };

            major.Click += WireType_Click;

            return major;
        }



        private static ToolStripMenuItem CreateLineMenu()
        {
            bool isOn = Grasshopper.Instances.Settings.GetValue(_wiretype, (int)_wiretypeDefault) == (int)WireTypes.Line;
            ToolStripMenuItem major = new ToolStripMenuItem("Line Wire", Properties.Resources.LineWireIcons_24)
            {
                Tag = WireTypes.Line,
                Checked = isOn,
            };

            CreateNumberBox(major, "End Extend Length", _lineExtend, _lineExtendDefault, 100, 0);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateNumberBox(major, "Corner Radius", _lineRadius, _lineRadiusDefault, 100, 0);

            major.Click += WireType_Click;
            return major;
        }

        private static ToolStripMenuItem CreatePolylineMenu()
        {
            bool isOn = Grasshopper.Instances.Settings.GetValue(_wiretype, (int)_wiretypeDefault) == (int)WireTypes.Polyline;
            ToolStripMenuItem major = new ToolStripMenuItem("Polyline Wire", Properties.Resources.PolylineWireIcons_24)
            {
                Tag = WireTypes.Polyline,
                Checked = isOn,
            };

            CreateNumberBox(major, "End Extend Length", _polylineExtend, _polylineExtendDefault, 100, 0);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateNumberBox(major, "Multiple of Corner Pitch", _polylineMulty, _polylineMultyDefault, 0.5, 0);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateNumberBox(major, "Corner Radius", _polylineRadius, _polylineRadiusDefault, 100, 0);

            major.Click += WireType_Click;
            return major;
        }

        private static void WireType_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in ((ToolStripMenuItem)sender).Owner.Items)
            {
                item.Checked = false;
            }
            ((ToolStripMenuItem)sender).Checked = true;

            Grasshopper.Instances.Settings.SetValue(_wiretype, (int)((ToolStripMenuItem)sender).Tag);
            Grasshopper.Instances.ActiveCanvas.Refresh();
        }

        public static void CreateNumberBox(ToolStripMenuItem item, string itemName, string valueName, double valueDefault, double Max, double Min)
        {
            item.DropDown.Closing -= DropDown_Closing;
            item.DropDown.Closing += DropDown_Closing;

            ToolStripLabel textBox = new ToolStripLabel(itemName);
            textBox.Font = new Font(textBox.Font.FontFamily, textBox.Font.Size, FontStyle.Bold);
            textBox.ToolTipText = $"Value from {Min} to {Max}";

            item.DropDownItems.Add(textBox);

            int decimalPlace = 3;

            GH_DigitScroller slider = new GH_DigitScroller
            {
                MinimumValue = (decimal)Min,
                MaximumValue = (decimal)Max,
                DecimalPlaces = decimalPlace,
                Value = (decimal)Grasshopper.Instances.Settings.GetValue(valueName, valueDefault),
                Size = new Size(150, 24),
            };
            slider.ValueChanged += Slider_ValueChanged;

            void Slider_ValueChanged(object sender, GH_DigitScrollerEventArgs e)
            {
                double result = (double)e.Value;
                result = result >= Min ? result : Min;
                result = result <= Max ? result : Max;
                Grasshopper.Instances.Settings.SetValue(valueName, result);
                slider.Value = (decimal)result;

                Grasshopper.Instances.ActiveCanvas.Refresh();
            }

            GH_DocumentObject.Menu_AppendCustomItem(item.DropDown, slider);

            //Add a Reset Item.
            ToolStripMenuItem resetItem = new ToolStripMenuItem("Reset Value", Properties.Resources.ResetIcons_24);

            resetItem.Click += ResetItem_Click;
            void ResetItem_Click(object sender, EventArgs e)
            {
                Grasshopper.Instances.Settings.SetValue(valueName, valueDefault);
                slider.Value = (decimal)valueDefault;
                Grasshopper.Instances.ActiveCanvas.Refresh();
            }
            item.DropDownItems.Add(resetItem);
        }

        private static void DropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
        }
    }
}

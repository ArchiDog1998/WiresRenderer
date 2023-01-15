using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WiresRenderer
{
    public static class MenuCreator
    {
        const string PREFIX = "WiresRenderer_";
        internal static readonly string _wiretype = PREFIX + "WireType";
        internal static readonly Wire_Types _wiretypeDefault = Wire_Types.Electric_Wire;

        internal static readonly string _bezierRatioX = PREFIX + "BezierRatioX";
        internal static readonly double _bezierRatioXDefault = 0.5;

        internal static readonly string _bezierRatioY = PREFIX + "BezierRatioY";
        internal static readonly double _bezierRatioYDefault = 0.75;

        internal static readonly string _bezierLength = PREFIX + "BezierExtend";
        internal static readonly double _bezierLengthDefault = 100;


        internal static readonly string _lineExtend = PREFIX + "LineExtend";
        internal static readonly double _lineExtendDefault = 3;

        internal static readonly string _lineRadius = PREFIX + "LineRadius";
        internal static readonly double _lineRadiusDefault = 8;

        internal static readonly string _polylineExtend = PREFIX + "PolylineExtend";
        internal static readonly double _polylineExtendDefault = 3;

        internal static readonly string _polylineMulty = PREFIX + "PolylineMulty";
        internal static readonly double _polylineMultyDefault = 0.3;

        internal static readonly string _polylineRadius = PREFIX + "PolylineRadius";
        internal static readonly double _polylineRadiusDefault = 12;

        internal static readonly string _electricExtend = PREFIX + "ElectricExtend";
        internal static readonly double _electricExtendDefault = 10;

        internal static readonly string _electricMulty = PREFIX + "ElectricMulty";
        internal static readonly double _electricMultyDefault = 1;

        internal static readonly string _electricRadius1 = PREFIX + "ElectricRadius1";
        internal static readonly double _electricRadius1Default = 100;

        internal static readonly string _electricRadius2 = PREFIX + "ElectricRadius2";
        internal static readonly double _electricRadius2Default = 12;

        internal static readonly string _wireWidth = PREFIX + "WireWidth";
        internal static readonly double _wireWidthDefault = 1;

        internal static readonly string _jumpToJumpTime = PREFIX + "JumpToJumpTime";
        internal static readonly int _jumpToJumpTimeDefault = 500;

        internal static readonly string _jumpToWaitTime = PREFIX + "JumpToWaitTime";
        internal static readonly int _jumpToWaitTimeDefault = 500;

        internal static readonly string _capsuleOffsetRadius = PREFIX + "CapsuleOffsetRadius";
        internal static readonly double _capsuleOffsetRadiusDefault = 2;

        public static ToolStripMenuItem CreateMajorMenu()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Wires Renderer", Properties.Resources.WiresRendererIcons_24, new ToolStripItem[]
            {
                CreateWireType(), CreateWireDefaultColor(), CreateWireSelectedColor(), CreateJumpTo(),
            }) { ToolTipText = "Change wire type or change wire width."};
            CreateNumberBox(major, "Multiple of Wire Width", _wireWidth, _wireWidthDefault, 10, 0.001);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateNumberBox(major, "Capsule Offset Distance", _capsuleOffsetRadius, _capsuleOffsetRadiusDefault, 20, 0, 3, true);

            return major;
        }

        private static ToolStripMenuItem CreateJumpTo()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Jump To Settings", new GH_JumpObject().Icon_24x24);
            major.DropDownItems.Add(CreateControlStateCheckBox<Jump_Type>(null));

            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateNumberBox(major, "Jump Time", _jumpToJumpTime, _jumpToJumpTimeDefault, 10000, 200, 0);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateNumberBox(major, "Wait Time", _jumpToWaitTime, _jumpToWaitTimeDefault, 10000, 200, 0);
            return major;
        }

        private static ToolStripMenuItem CreateWireDefaultColor()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Wire Default Color") { ToolTipText = "Change wire default color and empty color." };

            CreateColor(major, "Default Color", GH_Skin.wire_default, Color.FromArgb(150, 0, 0, 0), (color) => GH_Skin.wire_default = color);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            CreateColor(major, "Empty Color", GH_Skin.wire_empty, Color.FromArgb(180, 255, 60, 0), (color) => GH_Skin.wire_empty = color);

            return major;
        }

        private static ToolStripMenuItem CreateWireSelectedColor()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Wire Selected Color") { ToolTipText = "Change wire selected a color and selected b color." };

            GH_ColourPicker pickerA = CreateColor(major, "Selected A Color", GH_Skin.wire_selected_a, Color.FromArgb(255, 125, 210, 40), (color) => GH_Skin.wire_selected_a = color);
            GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
            GH_ColourPicker pickerB = CreateColor(major, "Selected B Color", GH_Skin.wire_selected_b, Color.FromArgb(50, 0, 0, 0), (color) => GH_Skin.wire_selected_b = color);

            ToolStripMenuItem toSameItem = new ToolStripMenuItem("Same To Selected A Color");
            toSameItem.Click += (sender, e) =>
            {
                pickerB.Colour = pickerA.Colour;
                GH_Skin.wire_selected_b = pickerA.Colour;
                Instances.ActiveCanvas.Refresh();
            };
            major.DropDownItems.Add(toSameItem);


            return major;
        }

        #region WireType Control
        private static ToolStripMenuItem CreateWireType()
        {
            ToolStripMenuItem major = new ToolStripMenuItem("Wire Types") { ToolTipText = "Provide a list of wire types" };
            major.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateRatioBezierMenu(), CreateExtendBezierMenu(), CreateLineMenu(), CreatePolylineMenu(), CreateElectricMenu(),
            });

            return major;
        }

        private static ToolStripMenuItem CreateRatioBezierMenu()
            => CreateWireTypeItem(Wire_Types.RatioBezier_Wire, "Bezier Ratio Wire", Properties.Resources.BezierWireIcons_24, (major) =>
            {
                CreateNumberBox(major, "Bezier X Ratio", _bezierRatioX, _bezierRatioXDefault, 1, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Bezier Y Ratio", _bezierRatioY, _bezierRatioYDefault, 1, 0);
            });

        private static ToolStripMenuItem CreateExtendBezierMenu()
            => CreateWireTypeItem(Wire_Types.LengthBezier_Wire, "Bezier Length Wire", Properties.Resources.BezierWireIcons_24, (major) =>
            {
                CreateNumberBox(major, "Bezier Length", _bezierLength, _bezierLengthDefault, 500, 0);
            });

        private static ToolStripMenuItem CreateLineMenu()
            => CreateWireTypeItem(Wire_Types.Line_Wire, "Line Wire", Properties.Resources.LineWireIcons_24, (major) =>
            {
                CreateNumberBox(major, "End Extend Length", _lineExtend, _lineExtendDefault, 100, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Corner Radius", _lineRadius, _lineRadiusDefault, 100, 0);
            });

        private static ToolStripMenuItem CreatePolylineMenu()
            => CreateWireTypeItem(Wire_Types.Polyline_Wire, "Polyline Wire", Properties.Resources.PolylineWireIcons_24, (major) =>
            {
                CreateNumberBox(major, "End Extend Length", _polylineExtend, _polylineExtendDefault, 100, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Multiple of Corner Pitch", _polylineMulty, _polylineMultyDefault, 1, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Corner Radius", _polylineRadius, _polylineRadiusDefault, 100, 0);
            });

        private static ToolStripMenuItem CreateElectricMenu()
            => CreateWireTypeItem(Wire_Types.Electric_Wire, "Electric Wire", Properties.Resources.ElectricWireIcons_24, (major) =>
            {
                CreateNumberBox(major, "End Extend Length", _electricExtend, _electricExtendDefault, 100, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Multiple of Corner Pitch", _electricMulty, _electricMultyDefault, 1, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Corner Radius 1", _electricRadius1, _electricRadius1Default, 1000, 0);
                GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                CreateNumberBox(major, "Corner Radius 2", _electricRadius2, _electricRadius2Default, 100, 0);
            });

        private static ToolStripMenuItem CreateWireTypeItem(Wire_Types type, string name, Image icon, Action<ToolStripMenuItem> addItems)
        {
            bool isOn = Instances.Settings.GetValue(_wiretype, (int)_wiretypeDefault) == (int)type;
            ToolStripMenuItem major = new ToolStripMenuItem(name, icon)
            {
                Tag = type,
                Checked = isOn,
            };

            major.Click += WireType_Click;

            addItems?.Invoke(major);
            return major;
        }

        private static void WireType_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in ((ToolStripMenuItem)sender).Owner.Items)
            {
                item.Checked = false;
            }
            ((ToolStripMenuItem)sender).Checked = true;

            Instances.Settings.SetValue(_wiretype, (int)((ToolStripMenuItem)sender).Tag);
            Instances.ActiveCanvas.Refresh();
        }
        #endregion

        #region Winform Controls
        private static GH_ColourPicker CreateColor(ToolStripMenuItem item, string itemName, Color rightColor, Color defaultColor, Action<Color> changeColor)
        {
            item.DropDown.Closing -= DropDown_Closing;
            item.DropDown.Closing += DropDown_Closing;

            ToolStripLabel textBox = new ToolStripLabel(itemName);
            textBox.Font = new Font(textBox.Font.FontFamily, textBox.Font.Size, FontStyle.Bold);
            item.DropDownItems.Add(textBox);

            GH_ColourPicker picker = GH_DocumentObject.Menu_AppendColourPicker(item.DropDown, rightColor , (sender, e) =>
            {
                changeColor.Invoke(e.Colour);
                Grasshopper.Instances.ActiveCanvas.Refresh();
            });

            //Add a Reset Item.
            ToolStripMenuItem resetItem = new ToolStripMenuItem("Reset Value", Properties.Resources.ResetIcons_24);
            resetItem.Click += (sender, e) =>
            {
                picker.Colour = defaultColor;
                changeColor.Invoke(defaultColor);
                Grasshopper.Instances.ActiveCanvas.Refresh();
            };
            item.DropDownItems.Add(resetItem);

            return picker;
        }

        private static ToolStripMenuItem CreateControlStateCheckBox<T>(Bitmap icon) where T : Enum
        {
            ToolStripMenuItem major = new ToolStripMenuItem(typeof(T).Name.Replace('_', ' '));
            if(icon != null) major.Image = icon;

            major.DropDown.Closing += DropDown_Closing;


            string saveKey = typeof(T).FullName;
            int current = Instances.Settings.GetValue(saveKey, 0);

            var enums = Enum.GetNames(typeof(T)).GetEnumerator();
            foreach (int i in Enum.GetValues(typeof(T)))
            {
                enums.MoveNext();
                string name = enums.Current.ToString().Replace('_', ' ');
                ToolStripMenuItem item = new ToolStripMenuItem(name) { Tag = i, Checked = i == current };
                item.Click += Item_Click;
                major.DropDownItems.Add(item);
            }

            void Item_Click(object sender, EventArgs e)
            {
                foreach (ToolStripMenuItem dropIt in major.DropDownItems)
                {
                    dropIt.Checked = false;
                }

                ToolStripMenuItem it = (ToolStripMenuItem)sender;
                it.Checked = true;
                Instances.Settings.SetValue(saveKey, (int)it.Tag);
            }

            return major;
        }

        private static void CreateNumberBox(ToolStripMenuItem item, string itemName, string valueName, double valueDefault, double Max, double Min, int decimalPlace = 3, bool expireLayout = false)
        {
            item.DropDown.Closing -= DropDown_Closing;
            item.DropDown.Closing +=  DropDown_Closing;

            ToolStripLabel textBox = new ToolStripLabel(itemName);
            textBox.TextAlign = ContentAlignment.MiddleCenter;
            textBox.Font = new Font(textBox.Font.FontFamily, textBox.Font.Size, FontStyle.Bold);
            textBox.ToolTipText = $"Value from {Min} to {Max}";
            item.DropDownItems.Add(textBox);


            Grasshopper.GUI.GH_DigitScroller slider = new Grasshopper.GUI.GH_DigitScroller
            {
                MinimumValue = (decimal)Min,
                MaximumValue = (decimal)Max,
                DecimalPlaces = decimalPlace,
                Value = (decimal)Instances.Settings.GetValue(valueName, valueDefault),
                Size = new Size(150, 24),
            };
            slider.ValueChanged += Slider_ValueChanged;

            void Slider_ValueChanged(object sender, GH_DigitScrollerEventArgs e)
            {
                double result = (double)e.Value;
                result = result >= Min ? result : Min;
                result = result <= Max ? result : Max;
                Instances.Settings.SetValue(valueName, result);
                slider.Value = (decimal)result;

                if (expireLayout)
                {
                    foreach (GH_Document doc in Instances.DocumentServer)
                    {
                        foreach (IGH_Attributes attr in doc.Attributes)
                        {
                            attr.ExpireLayout();
                        }
                    }
                }
                Instances.ActiveCanvas.Refresh();
            }

            GH_DocumentObject.Menu_AppendCustomItem(item.DropDown, slider);

            //Add a Reset Item.
            ToolStripMenuItem resetItem = new ToolStripMenuItem("Reset Value", Properties.Resources.ResetIcons_24);
            resetItem.Click += (sender, e) =>
            {
                Instances.Settings.SetValue(valueName, valueDefault);
                slider.Value = (decimal)valueDefault;

                if (expireLayout)
                {
                    foreach (GH_Document doc in Instances.DocumentServer)
                    {
                        foreach (IGH_Attributes attr in doc.Attributes)
                        {
                            attr.ExpireLayout();
                        }
                    }
                }
                Instances.ActiveCanvas.Refresh();
            };
            item.DropDownItems.Add(resetItem);
        }

        private static void DropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
        }

        #endregion
    }
}

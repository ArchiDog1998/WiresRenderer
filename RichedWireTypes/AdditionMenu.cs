using Grasshopper.Kernel;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Grasshopper;
using Grasshopper.GUI.Canvas.TagArtists;
using Grasshopper.GUI.Canvas;
using System.Drawing.Drawing2D;
using Grasshopper.GUI;
using Grasshopper.Kernel.Special;

namespace RichedWireTypes
{
    internal abstract class AdditionMenu : GH_ActiveObject
	{
        private static readonly MethodInfo _publishClick = typeof(GH_DocumentObject).GetRuntimeMethods().Where(m => m.Name.Contains("Menu_PublishObjectClick")).First();

        protected AdditionMenu(IGH_InstanceDescription tag) : base(tag)
        {

        }

		protected void Menu_AppendPublishNew(ToolStripDropDown menu)
        {
            if(this is IGH_Param)
            {
				IGH_Param param = (IGH_Param)this;
                for (int i = 0; i < menu.Items.Count; i++)
                {
                    var item = menu.Items[i];
                    if (item is ToolStripSeparator)
                    {
						ToolStripMenuItem additem = Menu_AppendJumpWires(param);
						if (additem != null)
							menu.Items.Insert(i, additem);
                        break;
                    }
                }
            }

            if (this is IRcpAwareObject)
            {
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Publish To Remote Panel");
                toolStripMenuItem.ToolTipText = "Publish this item to the remote control panel.";
                toolStripMenuItem.Click += (sender, e) => _publishClick.Invoke(this, new object[] { sender, e});
                menu.Items.Add(toolStripMenuItem);
            }
        }

		protected ToolStripMenuItem Menu_AppendJumpWires(IGH_Param param)
		{
			int sourcesCount = param.Sources.Count;
			int recipientsCount = param.Recipients.Count;
			if (sourcesCount + recipientsCount == 0)
			{
				return null;
			}
			ToolStripMenuItem major = new ToolStripMenuItem("Jump To", new GH_JumpObject().Icon_24x24);
            if (sourcesCount > 0)
            {
                if (param.Kind == GH_ParamKind.floating)
                {
                    ToolStripMenuItem toolStripMenuItem2 = GH_DocumentObject.Menu_AppendItem(major.DropDown, "Input:");
                    toolStripMenuItem2.Enabled = false;
                    toolStripMenuItem2.TextAlign = ContentAlignment.MiddleCenter;
                    toolStripMenuItem2.Font = GH_FontServer.NewFont(toolStripMenuItem2.Font, FontStyle.Italic);
                }
                foreach (IGH_Param source in param.Sources)
                {
                    ToolStripMenuItem sourcesItem = GH_DocumentObject.Menu_AppendItem(major.DropDown, source.Attributes.PathName, Menu_JumpClicked, source.Icon_24x24, enabled: true, @checked: false);
                    sourcesItem.Tag = source;
                    sourcesItem.MouseEnter += Menu_JumpSourceMouseEnter;
                    sourcesItem.MouseLeave += Menu_JumpMouseLeave;
                }
            }
            if (recipientsCount > 0)
            {
                if (param.Kind == GH_ParamKind.floating)
                {
                    GH_DocumentObject.Menu_AppendSeparator(major.DropDown);
                    ToolStripMenuItem toolStripMenuItem3 = GH_DocumentObject.Menu_AppendItem(major.DropDown, "Output:");
                    toolStripMenuItem3.Enabled = false;
                    toolStripMenuItem3.TextAlign = ContentAlignment.MiddleCenter;
                    toolStripMenuItem3.Font = GH_FontServer.NewFont(toolStripMenuItem3.Font, FontStyle.Italic);
                }
                foreach (IGH_Param recipient in param.Recipients)
                {
                    ToolStripMenuItem recipientItem = GH_DocumentObject.Menu_AppendItem(major.DropDown, recipient.Attributes.PathName, Menu_JumpClicked, recipient.Icon_24x24, enabled: true, @checked: false);
                    recipientItem.Tag = recipient;
                    recipientItem.MouseEnter += Menu_JumpRecipientMouseEnter;
                    recipientItem.MouseLeave += Menu_JumpMouseLeave;
                }
            }
            return major;
        }

        private void Menu_JumpClicked(object sender, EventArgs e)
        {
            Instances.ActiveCanvas.RemoveTagArtist(GH_TagArtist_WirePainter.WirePainter_ID);
            IGH_Param target = (IGH_Param)((ToolStripMenuItem)sender).Tag;

            GH_Canvas canvas = Grasshopper.Instances.ActiveCanvas;
            float width = canvas.Viewport.MidPoint.X - this.Attributes.Pivot.X;
            float height = canvas.Viewport.MidPoint.Y - this.Attributes.Pivot.Y;
            double num = GH_GraphicsUtil.Distance(this.Attributes.Pivot, target.Attributes.Pivot);

            RectangleF documentPort = canvas.Document.BoundingBox();
            documentPort.Inflate(5f, 5f);
            Rectangle screenPort = canvas.Viewport.ScreenPort;
            screenPort.Inflate(-5, -5);
            GH_NamedView zoomView = new GH_NamedView(screenPort, documentPort);

            GH_NamedView aimViewPort = new GH_NamedView();
            aimViewPort.Point = target.Attributes.Pivot + new SizeF(width, height);
            aimViewPort.Type = GH_NamedViewType.center;
            aimViewPort.Zoom = canvas.Viewport.Zoom;

            zoomView.SetToViewport(canvas, 500);
            canvas.Document.ScheduleSolution(500, (doc) =>
            {
                aimViewPort.SetToViewport(canvas, 500);
            });
        }

        private void Menu_JumpMouseLeave(object sender, EventArgs e)
        {
            Instances.ActiveCanvas.RemoveTagArtist(GH_TagArtist_WirePainter.WirePainter_ID);
            Instances.InvalidateCanvas();
        }

        private void Menu_JumpSourceMouseEnter(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
            if (toolStripMenuItem.Tag != null)
            {
                IGH_Param source = (IGH_Param)toolStripMenuItem.Tag;
                NewTag artist = new NewTag(source, (IGH_Param)this, Color.DarkBlue, 5);
                Instances.ActiveCanvas.AddTagArtist(artist);
                Instances.InvalidateCanvas();
            }
        }


        private void Menu_JumpRecipientMouseEnter(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
            if (toolStripMenuItem.Tag != null)
            {
                IGH_Param target = (IGH_Param)toolStripMenuItem.Tag;
                NewTag artist = new NewTag((IGH_Param)this, target, Color.DarkBlue, 5);
                Instances.ActiveCanvas.AddTagArtist(artist);
                Instances.InvalidateCanvas();
            }
        }
    }

    public class NewTag : GH_TagArtist_WirePainter
    {
        private static readonly FieldInfo _colourInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().Where(m => m.Name.Contains("m_colour")).First();
        private static readonly FieldInfo _widthInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().Where(m => m.Name.Contains("m_width")).First();
        private static readonly FieldInfo _sourceInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().Where(m => m.Name.Contains("m_source")).First();
        private static readonly FieldInfo _targetInfo = typeof(GH_TagArtist_WirePainter).GetRuntimeFields().Where(m => m.Name.Contains("m_target")).First();


        public NewTag(IGH_Param source, IGH_Param target, Color colour, int width)
            : base(source, target, colour, width)
        {

        }

        public NewTag(GH_TagArtist_WirePainter other):base(null, null, Color.DarkRed, 5)
        {
            _sourceInfo.SetValue(this, _sourceInfo.GetValue(other));
            _targetInfo.SetValue(this, _targetInfo.GetValue(other));
        }

        public override void Paint(GH_Canvas canvas, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires)
            {
                Pen pen = new Pen((Color)_colourInfo.GetValue(this), (int)_widthInfo.GetValue(this));
                GraphicsPath graphicsPath = WireDrawReplacer.GetDrawConnection(((IGH_Param)_sourceInfo.GetValue(this)).Attributes.OutputGrip,
                    ((IGH_Param)_targetInfo.GetValue(this)).Attributes.InputGrip, GH_WireDirection.right, GH_WireDirection.left);
                canvas.Graphics.DrawPath(pen, graphicsPath);
                graphicsPath.Dispose();
                pen.Dispose();
            }
        }
    }
}

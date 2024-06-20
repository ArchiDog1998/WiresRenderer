using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Canvas.TagArtists;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using HarmonyLib;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiresRenderer.Patch;

[HarmonyPatch(typeof(GH_ActiveObject))]
internal class ActiveObjectPatch
{
    [HarmonyPatch("Menu_AppendRuntimeMessages")]
    static void Prefix(GH_DocumentObject __instance, ToolStripDropDown menu)
    {
        if (__instance is not IGH_Param param) return;
        var item = Menu_AppendJumpWires(param);
        if (item == null) return;
        GH_DocumentObject.Menu_AppendSeparator(menu);
        menu.Items.Add(item);
    }

    static ToolStripMenuItem? Menu_AppendJumpWires(IGH_Param param)
    {
        int sourcesCount = param.Sources.Count;
        int recipientsCount = param.Recipients.Count;
        if (sourcesCount + recipientsCount == 0)
        {
            return null;
        }
        ToolStripMenuItem major = new ("Jump To", new GH_JumpObject().Icon_24x24);
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
                sourcesItem.Tag = (param, source);
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
                recipientItem.Tag = (param, recipient);
                recipientItem.MouseEnter += Menu_JumpRecipientMouseEnter;
                recipientItem.MouseLeave += Menu_JumpMouseLeave;
            }
        }
        return major;
    }

    private static bool _isJump = false;
    private static void Menu_JumpClicked(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menu) return;
        if (menu.Tag is not (IGH_Param from, IGH_Param target)) return;
        var fromAttribute = from.Attributes.GetTopLevel;

        _isJump = true;
        int jumpTime = (int)(Data.JumpTime * 1000);
        int waitTime = (int)(Data.WaitTime * 1000);

        GH_Canvas canvas = Instances.ActiveCanvas;
        float width = canvas.Viewport.MidPoint.X - fromAttribute.Pivot.X;
        float height = canvas.Viewport.MidPoint.Y - fromAttribute.Pivot.Y;
        double num = GH_GraphicsUtil.Distance(fromAttribute.Pivot, target.Attributes.Pivot);

        //ChangeSelected.
        Instances.ActiveCanvas.Document.DeselectAll();
        fromAttribute.Selected = target.Attributes.Selected = true;

        var current = Data.Jumptype;
        RectangleF documentPort = canvas.Document.BoundingBox(current == JumpType.TwoObjects);

        //Change Selected.
        Instances.ActiveCanvas.Document.DeselectAll();
        target.Attributes.Selected = true;

        documentPort.Inflate(5f, 5f);
        Rectangle screenPort = canvas.Viewport.ScreenPort;
        screenPort.Inflate(-5, -5);
        GH_NamedView zoomView = new (screenPort, documentPort);

        GH_NamedView aimViewPort = new()
        {
            Point = target.Attributes.Pivot + new SizeF(width, height),
            Type = GH_NamedViewType.center,
            Zoom = canvas.Viewport.Zoom
        };

        //Doing some jump.
        if (current == JumpType.DirectMove)
        {
            aimViewPort.SetToViewport(canvas, jumpTime);
            canvas.Document.ScheduleSolution(waitTime, (doc2) =>
            {
                Instances.ActiveCanvas.RemoveTagArtist(GH_TagArtist_WirePainter.WirePainter_ID);
                _isJump = false;
            });
        }
        else
        {
            zoomView.SetToViewport(canvas, jumpTime);
            canvas.Document.ScheduleSolution(waitTime, (doc) =>
            {
                aimViewPort.SetToViewport(canvas, jumpTime);
            });
            canvas.Document.ScheduleSolution(jumpTime + waitTime, (doc2) =>
            {
                Instances.ActiveCanvas.RemoveTagArtist(GH_TagArtist_WirePainter.WirePainter_ID);
                _isJump = false;
            });
        }
    }

    static void Menu_JumpMouseLeave(object? sender, EventArgs e)
    {
        if (_isJump) return;
        Instances.ActiveCanvas.RemoveTagArtist(GH_TagArtist_WirePainter.WirePainter_ID);
        Instances.InvalidateCanvas();
    }

    static void Menu_JumpSourceMouseEnter(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menu) return;
        if (menu.Tag is not (IGH_Param from, IGH_Param target)) return;

        var artist = new GH_TagArtist_WirePainter(target, from, Color.DarkBlue, 5);
        Instances.ActiveCanvas.AddTagArtist(artist);
        Instances.InvalidateCanvas();
    }

    static void Menu_JumpRecipientMouseEnter(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem menu) return;
        if (menu.Tag is not (IGH_Param from, IGH_Param target)) return;

        var artist = new GH_TagArtist_WirePainter(from, target, Color.DarkBlue, 5);
        Instances.ActiveCanvas.AddTagArtist(artist);
        Instances.InvalidateCanvas();
    }
}

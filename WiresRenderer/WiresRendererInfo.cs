using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WiresRenderer
{
    public class WiresRendererInfo : GH_AssemblyInfo
    {
        public override string Name => "Wires Renderer";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.WiresRendererIcons_24;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Provide a set of wire types such as Polyline. Also it can change wire's color and width.";

        public override Guid Id => new Guid("2850F61C-B64C-4466-8B9C-D89754AB6ECD");

        //Return a string identifying you or your company.
        public override string AuthorName => "秋水";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "1123993881@qq.com";

        public override string Version => "1.3.9";
    }

    public class SuperHelperAssemblyPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Instances.CanvasCreated -= Instances_CanvasCreated;

            GH_DocumentEditor editor = Instances.DocumentEditor;
            if (editor == null)
            {
                Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;
                return;
            }
            DoingSomethingFirst(editor);
        }

        private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
        {
            Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;

            GH_DocumentEditor editor = Instances.DocumentEditor;
            if (editor == null)
            {
                MessageBox.Show("WiresRenderer can't find the menu!");
                return;
            }
            DoingSomethingFirst(editor);
        }

        private void DoingSomethingFirst(GH_DocumentEditor editor)
        {
            ToolStripMenuItem displayItem = (ToolStripMenuItem)editor.MainMenuStrip.Items[3];
            displayItem.DropDownItems.Insert(3, MenuCreator.CreateMajorMenu());
            WireDrawReplacer.Init();
        }
    }
}
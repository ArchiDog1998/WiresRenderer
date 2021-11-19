using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RichedWireTypes
{
    public class RichedWireTypesInfo : GH_AssemblyInfo
    {
        public override string Name => "Riched Wire Types";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.RichedWireTypesIcons_24;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Provide a set of wire types such as Polyline.";

        public override Guid Id => new Guid("2850F61C-B64C-4466-8B9C-D89754AB6ECD");

        //Return a string identifying you or your company.
        public override string AuthorName => "秋水";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "1123993881@qq.com";

        public override string Version => "1.0.0";
    }

    public class SuperHelperAssemblyPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            //MessageBox.Show("Hello");
            Grasshopper.Instances.CanvasCreated += Instances_CanvasCreated;
            return GH_LoadingInstruction.Proceed;
        }

        private void Instances_CanvasCreated(GH_Canvas canvas)
        {
            Grasshopper.Instances.CanvasCreated -= Instances_CanvasCreated;

            GH_DocumentEditor editor = Grasshopper.Instances.DocumentEditor;
            if (editor == null)
            {
                Grasshopper.Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;
                return;
            }
            DoingSomethingFirst(editor);
        }

        private void ActiveCanvas_DocumentChanged(GH_Canvas sender, GH_CanvasDocumentChangedEventArgs e)
        {
            Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;

            GH_DocumentEditor editor = Grasshopper.Instances.DocumentEditor;
            if (editor == null)
            {
                MessageBox.Show("SuperHelper can't find the menu!");
                return;
            }
            DoingSomethingFirst(editor);
        }

        private void DoingSomethingFirst(GH_DocumentEditor editor)
        {
            var menu = editor.MainMenuStrip.Items.Find("mnuDisplay", false);
            if (menu.Length != 0)
            {
                var items = ((ToolStripMenuItem)menu[0]).DropDownItems;
                //if (items.Find("Sunglasses", false).Length == 0)
                    items.Insert(3, MenuCreator.CreateMajorMenu());
            }

            WireDrawReplacer.Init();
        }
    }
}
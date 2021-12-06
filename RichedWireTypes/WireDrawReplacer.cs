using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RichedWireTypes
{
    public enum WireTypes
    {
        Bezier,
        Line,
        Polyline,
    }
    internal class WireDrawReplacer : GH_Painter
    {
        private static MethodInfo generatePen = typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("GenerateWirePen")).First();

        protected WireDrawReplacer(GH_Canvas owner) : base(owner)
        {
        }

        public void NewDrawConnection(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB, bool selectedA, bool selectedB, GH_WireType type)
        {

            if (ConnectionVisible(pointA, pointB))
            {
                GraphicsPath graphicsPath = GetDrawConnection(pointA, pointB, directionA, directionB);
                Pen pen = GetPen(pointA, pointB, selectedA, selectedB, type);
                try
                {
                    Grasshopper.Instances.ActiveCanvas.Graphics.DrawPath(pen, graphicsPath);
                }
                catch
                {
                }
                graphicsPath.Dispose();
                pen.Dispose();
            }
        }

        private GraphicsPath GetDrawConnection(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
        {
            GraphicsPath graphicsPath;
            switch ((WireTypes)Grasshopper.Instances.Settings.GetValue(MenuCreator._wiretype, (int)MenuCreator._wiretypeDefault))
            {
                default:
                    graphicsPath = new GraphicsPath();
                    graphicsPath.AddLine(pointB, pointA);
                    break;

                case WireTypes.Bezier:
                    graphicsPath = ConnectionPath(pointA, pointB, directionA, directionB);
                    break;

                case WireTypes.Line:
                    graphicsPath = ConnectLine(pointA, pointB, directionA, directionB, (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._lineExtend, MenuCreator._lineExtendDefault),
                       (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._lineRadius, MenuCreator._lineRadiusDefault));
                    break;

                case WireTypes.Polyline:
                    graphicsPath = ConnectPolyline(pointA, pointB, directionA, directionB);
                    break;
            }
            return graphicsPath;
        }

        private Pen GetPen(PointF a, PointF b, bool asel, bool bsel, GH_WireType wiretype)
        {

            Pen pen = (Pen)generatePen.Invoke(this, new object[] { a, b, asel, bsel, wiretype });
            if (pen == null)
            {
                pen = new Pen(Color.Black);
            }
            pen.Width = pen.Width * (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._wireWidth, MenuCreator._wireWidthDefault);
            return pen;
        }

        private GraphicsPath ConnectLine(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB, float distance, float radius, bool isTrim = false)
        {
            PointF pointRight, pointLeft;
            if (directionA != GH_WireDirection.left)
            {
                pointRight = pointA;
                pointLeft = pointB;
            }
            else
            {
                pointRight = pointB;
                pointLeft = pointA;
            }

            GraphicsPath path = new GraphicsPath();

            if (pointRight.Y == pointLeft.Y && pointRight.X < pointLeft.X)
            {
                path.AddLine(pointRight, pointLeft);
            }
            else
            {
                PointF pointRightM = new PointF(pointRight.X + distance, pointRight.Y);
                PointF pointLeftM = new PointF(pointLeft.X - distance, pointLeft.Y);

                if (radius < 0)
                {
                    path.AddLine(pointRight, pointRightM);
                    path.AddLine(pointLeftM, pointLeft);
                }
                else
                {
                    //Change the radius and shrink the polyline.
                    if (isTrim)
                    {

                        //Distance that line end extend added.
                        float extendedDistance = distance - (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._polylineExtend, MenuCreator._polylineExtendDefault);

                        if (pointLeftM.Y == pointRightM.Y)
                        {
                            pointRightM = new PointF(pointRightM.X - extendedDistance, pointRightM.Y);
                            pointLeftM = new PointF(pointLeftM.X + extendedDistance, pointLeftM.Y);

                            radius = Math.Min(radius, GetMaxRadius(pointRightM, pointLeftM));
                        }
                        else
                        {
                            PointF CenterDir = new PointF(pointLeftM.X - pointRightM.X, Math.Abs(pointLeftM.Y - pointRightM.Y));

                            double centerDegree = Math.Atan(CenterDir.Y / CenterDir.X);
                            if (CenterDir.X < 0) centerDegree += Math.PI;

                            //Get Radius and Shrink by pitch's distance.
                            float tan = (float)Math.Tan(centerDegree / 2);
                            float halflength = (float)Math.Sqrt(Math.Pow(CenterDir.X, 2) + Math.Pow(CenterDir.Y, 2)) / 2;
                            float shouldRadius = (float)Math.Min(radius, halflength / tan - 0.001f);
                            float shouldShrink = tan * shouldRadius;


                            if (shouldShrink > extendedDistance)
                            {
                                pointRightM = new PointF(pointRightM.X - extendedDistance, pointRightM.Y);
                                pointLeftM = new PointF(pointLeftM.X + extendedDistance, pointLeftM.Y);

                                radius = Math.Min(radius, GetMaxRadius(pointRightM, pointLeftM));
                            }
                            else
                            {
                                pointRightM = new PointF(pointRightM.X - shouldShrink, pointRightM.Y);
                                pointLeftM = new PointF(pointLeftM.X + shouldShrink, pointLeftM.Y);

                                radius = shouldRadius - 0.001f;
                            }
                        }

                    }
                    else
                    {

                        radius = Math.Min(radius, GetMaxRadius(pointRightM, pointLeftM));
                    }

                    path.AddLine(pointRight, pointRightM);

                    if (radius > 0)
                    {
                        if (pointRight.Y >= pointLeft.Y)
                        {
                            PointF RightCenter = new PointF(pointRightM.X, pointRightM.Y - radius);
                            PointF LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y + radius);
                            PointF CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

                            double centerDegree = -Rhino.RhinoMath.ToDegrees(Math.Atan(CenterDir.Y / CenterDir.X));
                            if (CenterDir.X < 0) centerDegree += 180;

                            double additionalDegree = Rhino.RhinoMath.ToDegrees(Math.Asin(2 * radius / Math.Sqrt(Math.Pow(CenterDir.X, 2) + Math.Pow(CenterDir.Y, 2))));

                            float round = (float)(centerDegree + additionalDegree);

                            path.AddArc(pointRightM.X - radius, pointRightM.Y - 2 * radius, 2 * radius, 2 * radius, 90, -round);
                            path.AddArc(pointLeftM.X - radius, pointLeftM.Y, 2 * radius, 2 * radius, -90 - round, round);
                        }
                        else
                        {
                            PointF RightCenter = new PointF(pointRightM.X, pointRightM.Y + radius);
                            PointF LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y - radius);
                            PointF CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

                            double centerDegree = Rhino.RhinoMath.ToDegrees(Math.Atan(CenterDir.Y / CenterDir.X));
                            if (CenterDir.X < 0) centerDegree += 180;

                            double additionalDegree = Rhino.RhinoMath.ToDegrees(Math.Asin(2 * radius / Math.Sqrt(Math.Pow(CenterDir.X, 2) + Math.Pow(CenterDir.Y, 2))));

                            float round = (float)(centerDegree + additionalDegree);

                            path.AddArc(pointRightM.X - radius, pointRightM.Y, 2 * radius, 2 * radius, -90, round);
                            path.AddArc(pointLeftM.X - radius, pointLeftM.Y - 2 * radius, 2 * radius, 2 * radius, 90 + round, -round);
                        }
                    }

                    path.AddLine(pointLeftM, pointLeft);
                }
            }

            if (directionA != GH_WireDirection.left) path.Reverse();
            return path;
        }

        private float GetMaxRadius(PointF pointRight, PointF pointLeft)
        {
            double u = Math.Abs(pointRight.X - pointLeft.X);
            double v = Math.Abs(pointRight.Y - pointLeft.Y);

            return (float)((Math.Pow(u, 2) + Math.Pow(v, 2)) / v / 4) - 0.001f;
        }

        private GraphicsPath ConnectPolyline(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
        {
            float distance = Math.Abs(pointB.X - pointA.X) * (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._polylineMulty, MenuCreator._polylineMultyDefault);
            float radius = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._polylineRadius, MenuCreator._polylineRadiusDefault);
            return ConnectLine(pointA, pointB, directionA, directionB, distance, radius, true);
        }

        private float DistanceToWireNew(PointF locus, float radius, PointF source, PointF target)
        {
            GraphicsPath graphicsPath = GetDrawConnection(source, target, GH_WireDirection.right, GH_WireDirection.left);
            graphicsPath.Flatten();
            PointF[] points = graphicsPath.PathPoints;
            float min = float.MaxValue;
            for (int i = 0; i < points.Length - 1; i++)
            {
                min = Math.Min(min, pointToLine(points[i], points[i + 1], locus));
            }
            return min;
        }

        private static float distance(PointF p, PointF p1)
        {
            return (float)Math.Sqrt(Math.Pow(p.X - p1.X, 2) + Math.Pow(p.Y - p1.Y, 2));
        }
        private static float pointToLine(PointF p1, PointF p2, PointF p)
        {
            float a, b, c;
            a = distance(p1, p2);
            b = distance(p1, p);
            c = distance(p2, p);
            if (c + b == a)
            {
                return 0;
            }
            if (a <= 0.00001)
            {
                return b;
            }
            if (c * c >= a * a + b * b)
            {
                return b;
            }
            if (b * b >= a * a + c * c)
            {
                return c;
            }
            float p0 = (a + b + c) / 2;
            float s = (float)Math.Sqrt(p0 * (p0 - a) * (p0 - b) * (p0 - c));
            return 2 * s / a;
        }


        public static bool Init()
        {
            ExchangeMethod(
                typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("DrawConnection")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains("NewDrawConnection")).First()
                );
            ExchangeMethod(
                typeof(GH_Document).GetRuntimeMethods().Where(m => m.Name.Contains("DistanceToWire")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains("DistanceToWireNew")).First()
                );
            Grasshopper.Instances.ActiveCanvas.Document_ObjectsAdded += ActiveCanvas_Document_ObjectsAdded;
            return true;
        }

        private static void ActiveCanvas_Document_ObjectsAdded(GH_Document sender, GH_DocObjectEventArgs e)
        {
            if (e.ObjectCount != 1) return;
            IGH_DocumentObject obj = e.Objects[0];
            if (!(obj is GH_Relay)) return;

            sender.ScheduleSolution(50, (doc) =>
            {
                if (((GH_Relay)obj).SourceCount == 0) return;

                Point pointOnCanvas = Grasshopper.Instances.ActiveCanvas.PointToClient(Control.MousePosition);
                PointF pointOnDocument = Grasshopper.Instances.ActiveCanvas.Viewport.UnprojectPoint(pointOnCanvas);
                obj.Attributes.Pivot = pointOnDocument;
            });
        }

        private static bool ExchangeMethod(MethodInfo targetMethod, MethodInfo injectMethod)
        {
            if (targetMethod == null || injectMethod == null)
            {
                return false;
            }
            RuntimeHelpers.PrepareMethod(targetMethod.MethodHandle);
            RuntimeHelpers.PrepareMethod(injectMethod.MethodHandle);
            unsafe
            {
                if (IntPtr.Size == 4)
                {
                    int* tar = (int*)targetMethod.MethodHandle.Value.ToPointer() + 2;
                    int* inj = (int*)injectMethod.MethodHandle.Value.ToPointer() + 2;
                    var relay = *tar;
                    *tar = *inj;
                    *inj = relay;
                }
                else
                {
                    long* tar = (long*)targetMethod.MethodHandle.Value.ToPointer() + 1;
                    long* inj = (long*)injectMethod.MethodHandle.Value.ToPointer() + 1;
                    var relay = *tar;
                    *tar = *inj;
                    *inj = relay;
                }
            }
            return true;
        }
    }
}

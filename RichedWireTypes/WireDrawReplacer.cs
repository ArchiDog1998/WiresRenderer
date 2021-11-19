using Grasshopper.GUI.Canvas;
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
    internal abstract class WireDrawReplacer : GH_Painter
    {
        private static readonly MethodInfo generatePen = typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("GenerateWirePen")).First();

        protected WireDrawReplacer(GH_Canvas owner) : base(owner)
        {
        }

        public void NewDrawConnection(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB, bool selectedA, bool selectedB, GH_WireType type)
        {

            if (ConnectionVisible(pointA, pointB))
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
                Pen pen = (Pen)generatePen.Invoke(this, new object[] { pointA, pointB, selectedA, selectedB, type });
                if(pen == null)
                {
                    pen = new Pen(Color.Black);
                }
                pen.Width = pen.Width * (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._wireWidth, MenuCreator._wireWidthDefault);
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

            if(pointRight.Y == pointLeft.Y && pointRight.X < pointLeft.X)
            {
                path.AddLine(pointRight, pointLeft);
            }
            else
            {
                PointF pointRightM = new PointF(pointRight.X + distance, pointRight.Y);
                PointF pointLeftM = new PointF(pointLeft.X - distance, pointLeft.Y);

                if(radius < 0)
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

                    if(radius > 0)
                    {
                        if (pointRight.Y >= pointLeft.Y)
                        {
                            PointF RightCenter = new PointF(pointRightM.X, pointRightM.Y - radius);
                            PointF LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y + radius);
                            PointF CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

                            double centerDegree = - Rhino.RhinoMath.ToDegrees(Math.Atan(CenterDir.Y / CenterDir.X));
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

        public static bool Init()
        {
            return ExchangeMethod(
                typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("DrawConnection")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains("NewDrawConnection")).First()
                );
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

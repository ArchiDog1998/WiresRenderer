using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.Canvas.TagArtists;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WiresRenderer
{

    internal class WireDrawReplacer : GH_Painter
    {
        private static MethodInfo generatePen = typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("GenerateWirePen")).First();
        private static FieldInfo artTags = typeof(GH_Canvas).GetRuntimeFields().Where(m => m.Name.Contains("_artists")).First();
        const float TOLERANCE = 0.001f;

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

        public static GraphicsPath GetDrawConnection(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
        {
            GraphicsPath graphicsPath;
            switch ((Wire_Types)Grasshopper.Instances.Settings.GetValue(MenuCreator._wiretype, (int)MenuCreator._wiretypeDefault))
            {
                default:
                    graphicsPath = new GraphicsPath();
                    graphicsPath.AddLine(pointB, pointA);
                    break;

                case Wire_Types.RatioBezier_Wire:
                    graphicsPath = ConnectRatioBezier(pointA, pointB, directionA, directionB);
                    break;

                case Wire_Types.LengthBezier_Wire:
                    graphicsPath = ConnectLengthBezier(pointA, pointB, directionA, directionB);
                    break;

                case Wire_Types.Line_Wire:
                    graphicsPath = ConnectLine(pointA, pointB, directionA, directionB);
                    break;

                case Wire_Types.Polyline_Wire:
                    graphicsPath = ConnectPolyline(pointA, pointB, directionA, directionB);
                    break;

                case Wire_Types.Electric_Wire:
                    graphicsPath = ConnectElectric(pointA, pointB, directionA, directionB);
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

        #region Connctions
        private static GraphicsPath ConnectRatioBezier(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
            => ConnectPathDirection(pointA, pointB, directionA, directionB, (pointRight, pointLeft) =>
            {
                var ratioX = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._bezierRatioX, MenuCreator._bezierRatioXDefault);
                var ratioY = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._bezierRatioY, MenuCreator._bezierRatioYDefault);

                var movement = Math.Max(ratioX * Math.Abs(pointRight.X - pointLeft.X), ratioY * Math.Abs(pointRight.Y - pointLeft.Y));

                var path = new GraphicsPath();
                path.AddBezier(pointRight, new PointF(pointRight.X + movement, pointRight.Y), new PointF(pointLeft.X - movement, pointLeft.Y),  pointLeft);
                return path;
            });

        private static GraphicsPath ConnectLengthBezier(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
            => ConnectPathDirection(pointA, pointB, directionA, directionB, (pointRight, pointLeft) =>
            {
                var movement = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._bezierLength, MenuCreator._bezierLengthDefault);

                var path = new GraphicsPath();
                path.AddBezier(pointRight, new PointF(pointRight.X + movement, pointRight.Y), new PointF(pointLeft.X - movement, pointLeft.Y), pointLeft);
                return path;
            });

        private static GraphicsPath ConnectLine(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
            => ConnectPathDirection(pointA, pointB, directionA, directionB, (pointRight, pointLeft) =>
            
            {
                var extend = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._lineExtend, MenuCreator._lineExtendDefault);
                var radius = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._lineRadius, MenuCreator._lineRadiusDefault);
            
                var path = ConnectPolylineUnTrim(pointRight, pointLeft, extend, extend, radius, radius);
            
                return path;
            });

        private static GraphicsPath ConnectPolyline(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
            => ConnectPathDirection(pointA, pointB, directionA, directionB, (pointRight, pointLeft) =>
            {
                float distance = (pointLeft.X - pointRight.X) *
                    (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._polylineMulty, MenuCreator._polylineMultyDefault);

                var extend = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._polylineExtend, MenuCreator._polylineExtendDefault);

                float radius = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._polylineRadius, MenuCreator._polylineRadiusDefault);

                return ConnectPolylineTrim(pointRight, pointLeft, distance, distance, extend, extend, radius, radius, false);
            });

        private static GraphicsPath ConnectElectric(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
            => ConnectPathDirection(pointA, pointB, directionA, directionB, (pointRight, pointLeft) =>
            {
                var extend = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._electricExtend, MenuCreator._electricExtendDefault);
                var radiusRight = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._electricRadius1, MenuCreator._electricRadius1Default);
                var radiusLeft = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._electricRadius2, MenuCreator._electricRadius2Default);

                var multy = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._electricMulty, MenuCreator._electricMultyDefault);
                var alpha = Math.Atan(multy);

                var half = (Math.PI/ 2 - alpha) /2;
                var distanceLeft = (float)Math.Tan(half) * radiusLeft;


                float distancRight;
                if (pointLeft.X <= pointRight.X)
                {
                    distancRight = distanceLeft;
                }
                else
                {
                    var horizon = Math.Abs(pointRight.X - pointLeft.X);
                    var vertical = Math.Abs(pointRight.Y - pointLeft.Y);
                    distancRight = Math.Max(distanceLeft, horizon - distanceLeft - vertical * multy);
                }

                return ConnectPolylineTrim(pointRight, pointLeft, distancRight, distanceLeft, extend, extend, radiusRight, radiusLeft, true);
            });

        private static GraphicsPath ConnectPathDirection(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB, Func<PointF, PointF, GraphicsPath> getBezier)
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

            var path = getBezier.Invoke(pointRight, pointLeft);

            if (directionA != GH_WireDirection.left) path.Reverse();
            return path;
        }
        #endregion


        #region BasicPolyline


        delegate (float radiusRight, float radiusLeft, PointF pointRightM, PointF pointLeftM) ChangeRadiusPointMFunc(float radiusRight, float radiusLeft, PointF pointRightM, PointF pointLeftM);
        private static GraphicsPath ConnectPolylineTrim(PointF pointRight, PointF pointLeft, float distanceRight, float distanceLeft, float distanceRightLeast, float distanceLeftLeast,
            float radiusRight, float radiusLeft, bool adjustRightRadius) => ConnectPolyineBasic(pointRight, pointLeft, distanceRight, distanceLeft, radiusRight, radiusLeft, (rR, rL, pRM, pLM) =>
            {
                ChangeRadiusAndPointM(ref rR, ref rL, ref pRM, ref pLM,
                    pointRight, pointLeft, distanceRightLeast, distanceLeftLeast, adjustRightRadius);
                return (rR, rL, pRM, pLM);
            });

        private static GraphicsPath ConnectPolylineUnTrim(PointF pointRight, PointF pointLeft, float distanceRight, float distanceLeft, float radiusRight, float radiusLeft)
            => ConnectPolyineBasic(pointRight, pointLeft, distanceRight, distanceLeft, radiusRight, radiusLeft, (rR, rL, pRM, pLM) =>
            {
                ChangeRadius(ref rR, ref rL, pRM, pLM, false);
                return (rR, rL, pRM, pLM);
            });

        private static GraphicsPath ConnectPolyineBasic(PointF pointRight, PointF pointLeft, float distanceRight, float distanceLeft, float radiusRight, float radiusLeft, ChangeRadiusPointMFunc changes)
        {
            GraphicsPath path = new GraphicsPath();

            if (pointRight.Y == pointLeft.Y && pointRight.X < pointLeft.X)
            {
                path.AddLine(pointRight, pointLeft);
            }
            else
            {
                distanceRight = Math.Max(distanceRight, 0);
                distanceLeft = Math.Max(distanceLeft, 0);
                PointF pointRightM = new PointF(pointRight.X + distanceRight, pointRight.Y);
                PointF pointLeftM = new PointF(pointLeft.X - distanceLeft, pointLeft.Y);

                if (radiusRight <= 0 && radiusLeft <= 0)
                {
                    path.AddLine(pointRight, pointRightM);
                    path.AddLine(pointLeftM, pointLeft);
                }
                else
                {
                    var res = changes(radiusRight, radiusLeft, pointRightM, pointLeftM);
                    radiusRight = res.radiusRight;
                    radiusLeft = res.radiusLeft;
                    pointRightM = res.pointRightM;
                    pointLeftM = res.pointLeftM;

                    path = DrawWholePolyline(pointRight, pointLeft, pointRightM, pointLeftM, radiusRight, radiusLeft);
                }
            }

            return path;
        }
        private static GraphicsPath DrawWholePolyline(PointF pointRight, PointF pointLeft, PointF pointRightM, PointF pointLeftM, float radiusRight, float radiusLeft)
        {
            GraphicsPath path = new GraphicsPath();

            path.AddLine(pointRight, pointRightM);

            AddArcOnPolyline(path, radiusRight, radiusLeft, pointRightM, pointLeftM, pointRight.Y >= pointLeft.Y);

            path.AddLine(pointLeftM, pointLeft);

            return path;
        }

        private static void AddArcOnPolyline(GraphicsPath path, float radiusRight, float radiusLeft, 
            PointF pointRightM, PointF pointLeftM, bool rightPtOnBottom)
        {
            if (rightPtOnBottom)
            {
                PointF RightCenter = new PointF(pointRightM.X, pointRightM.Y - radiusRight);
                PointF LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y + radiusLeft);
                PointF CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

                double centerDegree = CenterDir.X == 0 ? 90
                    : - Rhino.RhinoMath.ToDegrees(Math.Atan(CenterDir.Y / CenterDir.X));
                if (CenterDir.X < 0) centerDegree += 180;

                double additionalDegree = Rhino.RhinoMath.ToDegrees(Math.Asin((radiusRight + radiusLeft) 
                    / Math.Sqrt(Math.Pow(CenterDir.X, 2) + Math.Pow(CenterDir.Y, 2))));

                float round = (float)(centerDegree + additionalDegree);

                if (radiusRight > 0) path.AddArc(pointRightM.X - radiusRight, pointRightM.Y - 2 * radiusRight, 2 * radiusRight, 2 * radiusRight, 90, -round);
                if (radiusLeft > 0) path.AddArc(pointLeftM.X - radiusLeft, pointLeftM.Y, 2 * radiusLeft, 2 * radiusLeft, -90 - round, round);
            }
            else
            {
                PointF RightCenter = new PointF(pointRightM.X, pointRightM.Y + radiusRight);
                PointF LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y - radiusLeft);
                PointF CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

                double centerDegree = CenterDir.X == 0 ? 90
                    : Rhino.RhinoMath.ToDegrees(Math.Atan(CenterDir.Y / CenterDir.X));
                if (CenterDir.X < 0) centerDegree += 180;

                double additionalDegree = Rhino.RhinoMath.ToDegrees(Math.Asin((radiusRight + radiusLeft)
                    / Math.Sqrt(Math.Pow(CenterDir.X, 2) + Math.Pow(CenterDir.Y, 2))));

                float round = (float)(centerDegree + additionalDegree);

                if (radiusRight > 0) path.AddArc(pointRightM.X - radiusRight, pointRightM.Y, 2 * radiusRight, 2 * radiusRight, -90, round);
                if (radiusLeft > 0) path.AddArc(pointLeftM.X - radiusLeft, pointLeftM.Y - 2 * radiusLeft, 2 * radiusLeft, 2 * radiusLeft, 90 + round, -round);
            }
        }
        #endregion

        #region Adjust PointM and Radius
        private static void ChangeRadiusAndPointM(ref float radiusRight, ref float radiusLeft, ref PointF pointRightM, ref PointF pointLeftM,
            PointF pointRight, PointF pointLeft, float distanceRightLeast, float distanceLeftLeast, bool adjustRightRadius)
        {
            ChangePointM(ref pointRightM, ref pointLeftM, radiusRight, radiusLeft);
            CheckPointM(ref pointRightM, ref pointLeftM, pointRight, pointLeft, distanceRightLeast, distanceLeftLeast);
            ChangeRadius(ref radiusRight, ref radiusLeft, pointRightM, pointLeftM, adjustRightRadius);
        }

        private static void ChangePointM(ref PointF pointRightM, ref PointF pointLeftM, float radiusRight, float radiusLeft)
        {
            double centerDegree = Math.Abs(Math.Atan(pointLeftM.Y - pointRightM.Y / pointLeftM.X - pointRightM.X));

            float tan = (float)Math.Tan(centerDegree / 2);
            float shouldShrinkRight = tan * radiusRight;
            float shouldShrinkLeft = tan * radiusLeft;

            pointRightM = new PointF(pointRightM.X - shouldShrinkRight, pointRightM.Y);
            pointLeftM = new PointF(pointLeftM.X + shouldShrinkLeft, pointLeftM.Y);
        }

        private static void CheckPointM(ref PointF pointRightM, ref PointF pointLeftM,
            PointF pointRight, PointF pointLeft, float distanceRightLeast, float distanceLeftLeast)
        {
            if (pointRightM.X < pointRight.X + distanceRightLeast)
            {
                pointRightM = new PointF(pointRight.X + distanceRightLeast, pointRight.Y);
            }
            if (pointLeftM.X > pointLeft.X - distanceLeftLeast)
            {
                pointLeftM = new PointF(pointLeft.X - distanceLeftLeast, pointLeft.Y);
            }
        }

        private static void ChangeRadius(ref float radiusRight, ref float radiusLeft, PointF pointRightM, PointF pointLeftM, bool adjustRightRadius)
        {
            if (adjustRightRadius)
            {
                radiusRight = Math.Min(radiusRight, Math.Max(radiusLeft, pointLeftM.X - pointRightM.X - radiusLeft));
            }

            var radiusAddition = GetMaxRadiusAddition(pointRightM, pointLeftM);

            if (radiusRight + radiusLeft <= radiusAddition) return;

            var ratio = radiusAddition / (radiusRight + radiusLeft);

            radiusRight *= ratio;
            radiusLeft *= ratio;
        }

        private static float GetMaxRadiusAddition(PointF pointRight, PointF pointLeft)
        {
            double u = Math.Abs(pointRight.X - pointLeft.X);
            double v = Math.Abs(pointRight.Y - pointLeft.Y);

            return (float)((Math.Pow(u, 2) + Math.Pow(v, 2)) / v / 4) * 2 - TOLERANCE;
        }
        #endregion

        #region Distance To Wire
        private float DistanceToWireNew(PointF locus, float radius, PointF source, PointF target)
        {
            GraphicsPath graphicsPath = GetDrawConnection(source, target, GH_WireDirection.right, GH_WireDirection.left);
            graphicsPath.Flatten();
            PointF[] points = graphicsPath.PathPoints;
            float min = float.MaxValue;
            for (int i = 0; i < points.Length - 1; i++)
            {
                min = Math.Min(min, PointToLine(points[i], points[i + 1], locus));
            }
            return min;
        }


        private static float PointToLine(PointF p1, PointF p2, PointF p)
        {
            float a, b, c;
            a = Distance(p1, p2);
            b = Distance(p1, p);
            c = Distance(p2, p);
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

        private static float Distance(PointF p, PointF p1)
            => (float)Math.Sqrt(Math.Pow(p.X - p1.X, 2) + Math.Pow(p.Y - p1.Y, 2));
        #endregion
        public static RectangleF LayoutBounds(IGH_Component owner, RectangleF bounds)
        {
            float offset = (float)Grasshopper.Instances.Settings.GetValue(MenuCreator._capsuleOffsetRadius, MenuCreator._capsuleOffsetRadiusDefault);
            foreach (IGH_Param param in owner.Params)
            {
                bounds = RectangleF.Union(bounds, param.Attributes.Bounds);
            }
            bounds.Inflate(offset, offset);

            foreach (IGH_Param param in owner.Params.Input)
            {
                param.Attributes.Bounds = new RectangleF(param.Attributes.Bounds.X - offset, param.Attributes.Bounds.Y, param.Attributes.Bounds.Width + offset, param.Attributes.Bounds.Height);
            }

            foreach (IGH_Param param in owner.Params.Output)
            {
                param.Attributes.Bounds = new RectangleF(param.Attributes.Bounds.X, param.Attributes.Bounds.Y, param.Attributes.Bounds.Width + offset, param.Attributes.Bounds.Height);
            }
            return bounds;
        }

        #region Init
        public static bool Init()
        {
            ExchangeMethod(
                typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("DrawConnection")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains(nameof(NewDrawConnection))).First()
                );

            ExchangeMethod(
                typeof(GH_Document).GetRuntimeMethods().Where(m => m.Name.Contains("DistanceToWire")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains(nameof(DistanceToWireNew))).First()
                );

            ExchangeMethod(
                typeof(GH_DocumentObject).GetRuntimeMethods().Where(m => m.Name.Contains("Menu_AppendPublish")).First(),
                typeof(AdditionMenu).GetRuntimeMethods().Where(m => m.Name.Contains("Menu_AppendPublishNew")).First()
                );

            ExchangeMethod(typeof(GH_ComponentAttributes).GetRuntimeMethods().Where(m => m.Name.Contains("LayoutBounds")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains(nameof(LayoutBounds))).First());

            Grasshopper.Instances.ActiveCanvas.Document_ObjectsAdded += ActiveCanvas_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.CanvasPaintBegin += ActiveCanvas_CanvasPaintBegin;
            return true;
        }

        private static void ActiveCanvas_CanvasPaintBegin(GH_Canvas sender)
        {
            List<IGH_TagArtist> tags = (List<IGH_TagArtist>)artTags.GetValue(sender);
            if (tags == null ||tags.Count == 0) return;

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] is GH_TagArtist_WirePainter && !(tags[i] is NewTag))
                {
                    tags[i] = new NewTag((GH_TagArtist_WirePainter)tags[i]);
                }
            }
            artTags.SetValue(sender, tags);
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
        #endregion
    }
}

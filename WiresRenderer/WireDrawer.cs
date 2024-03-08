using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using static Grasshopper.Rhinoceros.Display.Params.Param_ModelView;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace WiresRenderer;

internal static class WireDrawer
{
    private static readonly MethodInfo _generatePen = typeof(GH_Painter).GetRuntimeMethods().First(m => m.Name.Contains("GenerateWirePen"));
    private static readonly PropertyInfo _graphic = typeof(GH_Painter).GetRuntimeProperties().First(m => m.Name.Contains("Graphics"));
    const float TOLERANCE = 0.001f;

    public static void DrawConnection(GH_Painter painter, IGH_Param outputParam, IGH_Param inputParam, bool flag)
    {
        var pointRight = outputParam.Attributes.OutputGrip;
        var pointLeft = inputParam.Attributes.InputGrip;

        if (!painter.ConnectionVisible(pointRight, pointLeft)) return;

        var type = inputParam.WireDisplay;

        if (flag || type == GH_ParamWireDisplay.faint)
        {
            using var graphicsPath = GetPath(outputParam, inputParam);
            using var pen = GetPen(painter, pointRight, pointLeft, outputParam.Attributes.Selected, inputParam.Attributes.Selected,
                type == GH_ParamWireDisplay.faint && !flag ? GH_WireType.faint :
                CentralSettings.CanvasFancyWires ? GH_Painter.DetermineWireType(outputParam.VolatileData) : GH_WireType.generic);
            try
            {
                Instances.ActiveCanvas.Graphics.DrawPath(pen, graphicsPath);
                return;
            }
            catch
            {
            }
        }

        if (type == GH_ParamWireDisplay.hidden)
        {
            var graphic = _graphic.GetValue(painter) as Graphics;
            float num = graphic.Transform.Elements[0];
            if (num > 0.55f)
            {
                var point = GH_Convert.ToPoint(pointLeft);
                var alpha = GH_GraphicsUtil.BlendInteger(0.5, 1.0, 0, 80, num);
                int num2 = 0;
                do
                {
                    int num3 = 6 + 3 * num2;
                    var rect = new Rectangle(point.X - num3, point.Y - num3, 2 * num3, 2 * num3);
                    var linearGradientBrush = new LinearGradientBrush(rect, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(alpha, 0, 0, 0), LinearGradientMode.Vertical)
                    {
                        WrapMode = WrapMode.TileFlipXY,
                    };
                    linearGradientBrush.SetSigmaBellShape(0.5f);
                    using (var p = new Pen(linearGradientBrush, 1f))
                    {
                        graphic.DrawEllipse(p, rect);
                    }
                    linearGradientBrush.Dispose();
                    num2++;
                }
                while (num2 <= 3);
            }
        }
    }

    public static GraphicsPath GetPath(IGH_Param outputParam, IGH_Param inputParam)
    {
        var pointRight = outputParam.Attributes.OutputGrip;
        var pointLeft = inputParam.Attributes.InputGrip;

        GraphicsPath graphicsPath;
        switch (Data.Wiretype)
        {
            default:
                graphicsPath = new GraphicsPath();
                graphicsPath.AddLine(pointRight, pointLeft);
                break;

            case WireType.RatioBezierWire:
                graphicsPath = ConnectRatioBezier(pointRight, pointLeft);
                break;

            case WireType.LengthBezierWire:
                graphicsPath = ConnectLengthBezier(pointRight, pointLeft);
                break;

            case WireType.LineWire:
                graphicsPath = ConnectLine(pointRight, pointLeft);
                break;

            case WireType.PolylineWire:
                graphicsPath = ConnectPolyline(pointRight, pointLeft);
                break;

            case WireType.ElectricWire:
                graphicsPath = ConnectElectric(outputParam, inputParam, pointRight, pointLeft);
                break;
        }
        return graphicsPath;
    }

    private static Pen GetPen(GH_Painter painter, PointF a, PointF b, bool asel, bool bsel, GH_WireType wiretype)
    {

        Pen pen = (Pen)_generatePen.Invoke(painter, new object[] { a, b, asel, bsel, wiretype }) ?? new Pen(Color.Black);
        pen.Width *= Data.WireWidth;
        return pen;
    }

    #region Connctions
    private static GraphicsPath ConnectRatioBezier(PointF pointRight, PointF pointLeft)
    {
        var ratioX = Data.BezierRatioX;
        var ratioY = Data.BezierRatioY;

        var movement = Math.Max(ratioX * Math.Abs(pointRight.X - pointLeft.X), ratioY * Math.Abs(pointRight.Y - pointLeft.Y));

        var path = new GraphicsPath();
        path.AddBezier(pointRight, new PointF(pointRight.X + movement, pointRight.Y), new PointF(pointLeft.X - movement, pointLeft.Y), pointLeft);
        return path;
    }

    private static GraphicsPath ConnectLengthBezier(PointF pointRight, PointF pointLeft)
    {
        var movement = Data.BezierLength;

        var path = new GraphicsPath();
        path.AddBezier(pointRight, new PointF(pointRight.X + movement, pointRight.Y), new PointF(pointLeft.X - movement, pointLeft.Y), pointLeft);
        return path;
    }

    private static GraphicsPath ConnectLine(PointF pointRight, PointF pointLeft)
    {
        var extend = Data.LineExtend;
        var radius = Data.LineRadius;

        var path = ConnectPolylineUnTrim(pointRight, pointLeft, extend, extend, radius, radius);

        return path;
    }

    private static GraphicsPath ConnectPolyline(PointF pointRight, PointF pointLeft)
    {
        float distance = (pointLeft.X - pointRight.X) * Data.PolylineMulty;

        var extend = Data.PolylineExtend;
        float radius = Data.PolylineRadius;

        return ConnectPolylineTrim(pointRight, pointLeft, distance, distance, extend, extend, radius, radius, false);
    }

    private static float FromRatioToTan(float ratio) => (float)Math.Tan((Math.PI / 2 - Math.Atan(Math.Abs(ratio))) / 2);

    private static GraphicsPath ConnectElectric(IGH_Param outputParam, IGH_Param inputParam, PointF pointRight, PointF pointLeft)
    {
        var extend = Data.ElectricExtend;
        var radiusRight = Data.ElectricRadius1;
        var radiusLeft = Data.ElectricRadius2;
        var multy = Data.ElectricMulty;

        var distanceLeft = FromRatioToTan(multy) * radiusLeft + extend;

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
        GraphicsPath path = new();

        if (pointRight.Y == pointLeft.Y && pointRight.X < pointLeft.X)
        {
            path.AddLine(pointRight, pointLeft);
        }
        else
        {
            distanceRight = Math.Max(distanceRight, 0);
            distanceLeft = Math.Max(distanceLeft, 0);
            var pointRightM = new PointF(pointRight.X + distanceRight, pointRight.Y);
            var pointLeftM = new PointF(pointLeft.X - distanceLeft, pointLeft.Y);

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
        var path = new GraphicsPath();

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
            var RightCenter = new PointF(pointRightM.X, pointRightM.Y - radiusRight);
            var LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y + radiusLeft);
            var CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

            double centerDegree = CenterDir.X == 0 ? 90
                : -Rhino.RhinoMath.ToDegrees(Math.Atan(CenterDir.Y / CenterDir.X));
            if (CenterDir.X < 0) centerDegree += 180;

            double additionalDegree = Rhino.RhinoMath.ToDegrees(Math.Asin((radiusRight + radiusLeft)
                / Math.Sqrt(Math.Pow(CenterDir.X, 2) + Math.Pow(CenterDir.Y, 2))));

            float round = (float)(centerDegree + additionalDegree);

            if (radiusRight > 0) path.AddArc(pointRightM.X - radiusRight, pointRightM.Y - 2 * radiusRight, 2 * radiusRight, 2 * radiusRight, 90, -round);
            if (radiusLeft > 0) path.AddArc(pointLeftM.X - radiusLeft, pointLeftM.Y, 2 * radiusLeft, 2 * radiusLeft, -90 - round, round);
        }
        else
        {
            var RightCenter = new PointF(pointRightM.X, pointRightM.Y + radiusRight);
            var LeftCenter = new PointF(pointLeftM.X, pointLeftM.Y - radiusLeft);
            var CenterDir = new PointF(LeftCenter.X - RightCenter.X, LeftCenter.Y - RightCenter.Y);

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
        var tanOrg = (pointLeftM.X - pointRightM.X) / (pointLeftM.Y - pointRightM.Y);
        float tan = FromRatioToTan(tanOrg);
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
            pointRightM = new (pointRight.X + distanceRightLeast, pointRight.Y);
        }
        if (pointLeftM.X > pointLeft.X - distanceLeftLeast)
        {
            pointLeftM = new (pointLeft.X - distanceLeftLeast, pointLeft.Y);
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
}
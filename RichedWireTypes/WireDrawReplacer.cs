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

namespace RichedWireTypes
{
    internal abstract class WireDrawReplacer : GH_Painter
    {
        protected WireDrawReplacer(GH_Canvas owner) : base(owner)
        {
        }

        public static GraphicsPath NewConnectionPath(PointF pointA, PointF pointB, GH_WireDirection directionA, GH_WireDirection directionB)
        {
            //BezierF bezierF = ((directionA != 0) ? ConnectionPathBezier(pointA, pointB).Reverse() : ConnectionPathBezier(pointB, pointA));
            GraphicsPath graphicsPath = new GraphicsPath();
            graphicsPath.AddLine(pointA, pointB);
            return graphicsPath;
        }

        public static bool Init()
        {
            return ExchangeMethod(
                typeof(GH_Painter).GetRuntimeMethods().Where(m => m.Name.Contains("ConnectionPath")).First(),
                typeof(WireDrawReplacer).GetRuntimeMethods().Where(m => m.Name.Contains("NewConnectionPath")).First()
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

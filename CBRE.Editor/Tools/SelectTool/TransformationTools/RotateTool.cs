using CBRE.Editor.Documents;
using CBRE.Editor.Tools.Widgets;
using CBRE.Extensions;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using CBRE.DataStructures.Geometric;

namespace CBRE.Editor.Tools.SelectTool.TransformationTools
{
    /// <summary>
    /// Allows the selected objects to be rotated
    /// </summary>
    class RotateTool : TransformationTool
    {
        public override bool RenderCircleHandles
        {
            get { return true; }
        }

        public override bool FilterHandle(BaseBoxTool.ResizeHandle handle)
        {
            return handle == BaseBoxTool.ResizeHandle.BottomLeft
                   || handle == BaseBoxTool.ResizeHandle.BottomRight
                   || handle == BaseBoxTool.ResizeHandle.TopLeft
                   || handle == BaseBoxTool.ResizeHandle.TopRight;
        }

        public override string GetTransformName()
        {
            return "Rotate";
        }

        public override MouseCursor CursorForHandle(BaseBoxTool.ResizeHandle handle)
        {
            return GameMain.Instance.RotateCursor;
        }

        #region 2D Transformation Matrix
        public override Matrix GetTransformationMatrix(Viewport2D viewport, ViewportEvent e, BaseBoxTool.BoxState state, Document doc, IEnumerable<Widget> activeWidgets)
        {
            var origin = viewport.ZeroUnusedCoordinate((state.PreTransformBoxStart + state.PreTransformBoxEnd) / 2);
            var rw = activeWidgets.OfType<RotationWidget>().FirstOrDefault();
            if (rw != null) origin = rw.GetPivotPoint();

            var forigin = viewport.Flatten(origin);

            var origv = (state.MoveStart - forigin).Normalise();
            var newv = (viewport.ScreenToWorld(e.X, viewport.Height - e.Y) - forigin).Normalise();

            var angle = DMath.Acos(Math.Max(-1, Math.Min(1, origv.Dot(newv))));
            if ((origv.Cross(newv).Z < 0)) angle = 2 * DMath.PI - angle;

            var shf = ViewportManager.Shift;
            var def = Select.RotationStyle;
            var snap = (def == RotationStyle.SnapOnShift && shf) || (def == RotationStyle.SnapOffShift && !shf);
            if (snap)
            {
                var deg = angle * (180 / DMath.PI);
                var rnd = Math.Round(deg / 15) * 15;
                angle = rnd * (DMath.PI / 180);
            }

            Matrix rotm;
            if (viewport.Direction == Viewport2D.ViewDirection.Top) rotm = Matrix.RotationZ(-angle);
            else if (viewport.Direction == Viewport2D.ViewDirection.Front) rotm = Matrix.RotationX(-angle);
            else rotm = Matrix.RotationY(angle); // The Y axis rotation goes in the reverse direction for whatever reason

            var mov = Matrix.Translation(origin);
            var rot = mov * rotm;
            return rot * mov.Inverse();
        }
        #endregion 2D Transformation Matrix

        public override IEnumerable<Widget> GetWidgets(Document document)
        {
            yield return new RotationWidget(document);
        }
    }
}

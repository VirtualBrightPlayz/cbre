using CBRE.DataStructures.Geometric;
using CBRE.Editor.Documents;
using CBRE.Editor.Tools.Widgets;
using CBRE.Editor.Rendering;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Tools.SelectTool.TransformationTools
{
    /// <summary>
    /// Allows the selected objects to be skewed
    /// </summary>
    class SkewTool : TransformationTool
    {
        public override bool RenderCircleHandles
        {
            get { return false; }
        }

        public override bool FilterHandle(BaseBoxTool.ResizeHandle handle)
        {
            return handle == BaseBoxTool.ResizeHandle.Bottom
                   || handle == BaseBoxTool.ResizeHandle.Left
                   || handle == BaseBoxTool.ResizeHandle.Top
                   || handle == BaseBoxTool.ResizeHandle.Right;
        }

        public override string GetTransformName()
        {
            return "Skew";
        }

        public override MouseCursor CursorForHandle(BaseBoxTool.ResizeHandle handle)
        {
            return (handle == BaseBoxTool.ResizeHandle.Top || handle == BaseBoxTool.ResizeHandle.Bottom)
                       ? MouseCursor.SizeWE
                       : MouseCursor.SizeNS;
        }

        #region 2D Transformation Matrix
        public override Matrix GetTransformationMatrix(Viewport2D viewport, ViewportEvent e, BaseBoxTool.BoxState state, Document doc, IEnumerable<Widget> activeWidgets)
        {
            var shearUpDown = state.Handle == BaseBoxTool.ResizeHandle.Left || state.Handle == BaseBoxTool.ResizeHandle.Right;
            var shearTopRight = state.Handle == BaseBoxTool.ResizeHandle.Top || state.Handle == BaseBoxTool.ResizeHandle.Right;

            var nsmd = viewport.ScreenToWorld(e.X, viewport.Height - e.Y) - state.MoveStart;
            var mouseDiff = SnapIfNeeded(nsmd, doc);
            if (ViewportManager.Shift)
            {
                mouseDiff = doc.Snap(nsmd, doc.Map.GridSpacing / 2);
            }

            var relative = viewport.Flatten(state.PreTransformBoxEnd - state.PreTransformBoxStart);
            var shearOrigin = (shearTopRight) ? state.PreTransformBoxStart : state.PreTransformBoxEnd;

            var shearAmount = new Vector3(mouseDiff.X / relative.Y, mouseDiff.Y / relative.X, 0);
            if (!shearTopRight) shearAmount *= -1;

            var shearMatrix = Matrix.Identity;
            var sax = shearAmount.X;
            var say = shearAmount.Y;

            switch (viewport.Direction)
            {
                case Viewport2D.ViewDirection.Top:
                    if (shearUpDown) shearMatrix[(1 * 4) + 0] = say;
                    else shearMatrix[(0 * 4) + 1] = sax;
                    break;
                case Viewport2D.ViewDirection.Front:
                    if (shearUpDown) shearMatrix[(2 * 4) + 1] = say;
                    else shearMatrix[(1 * 4) + 2] = sax;
                    break;
                case Viewport2D.ViewDirection.Side:
                    if (shearUpDown) shearMatrix[(2 * 4) + 0] = say;
                    else shearMatrix[(0 * 4) + 2] = sax;
                    break;
            }


            var stran = Matrix.Translation(shearOrigin);
            var shear = stran * shearMatrix;
            return shear * stran.Inverse();
        }
        #endregion 2D Transformation Matrix

        public override IEnumerable<Widget> GetWidgets(Document document)
        {
            yield break;
        }
    }
}

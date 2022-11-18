﻿using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Tools.Widgets;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Tools.SelectTool.TransformationTools
{
    /// <summary>
    /// Allows the selected objects to be scaled and translated
    /// </summary>
    class ResizeTool : TransformationTool
    {

        public override bool RenderCircleHandles
        {
            get { return false; }
        }

        public override bool FilterHandle(BaseBoxTool.ResizeHandle handle)
        {
            return true;
        }

        public override string GetTransformName()
        {
            return "Resize";
        }

        public override MouseCursor CursorForHandle(BaseBoxTool.ResizeHandle handle)
        {
            return null;
        }

        #region 2D Transformation Matrix
        public override Matrix GetTransformationMatrix(Viewport2D viewport, ViewportEvent e, BaseBoxTool.BoxState state, Document doc, IEnumerable<Widget> activeWidgets)
        {
            var coords = GetBoxCoordinatesForSelectionResize(viewport, e, state, doc);
            state.BoxStart = coords.Item1;
            state.BoxEnd = coords.Item2;
            Matrix resizeMatrix;
            if (state.Handle == BaseBoxTool.ResizeHandle.Center)
            {
                var movement = state.BoxStart - state.PreTransformBoxStart;
                resizeMatrix = Matrix.Translation(movement ?? Vector3.Zero);
            }
            else
            {
                var resize = ((state.PreTransformBoxStart - state.BoxStart) +
                             (state.BoxEnd - state.PreTransformBoxEnd)) ?? Vector3.Zero;
                resize = resize.ComponentDivide((state.PreTransformBoxEnd - state.PreTransformBoxStart) ?? Vector3.One);
                resize += new Vector3(1, 1, 1);
                var offset = GetOriginForTransform(viewport, state);
                var trans = Matrix.Translation(offset);
                var scale = trans * Matrix.Scale(resize);
                resizeMatrix = scale * trans.Inverse();
            }
            return resizeMatrix;
        }

        private Vector3? GetResizeOrigin(Viewport2D viewport, BaseBoxTool.BoxState state, Document document)
        {
            if (state.Action != BaseBoxTool.BoxAction.Resizing || state.Handle != BaseBoxTool.ResizeHandle.Center) { return null; }
            var sel = document.Selection.GetSelectedParents().ToList();
            if (sel.Count == 1 && sel[0] is Entity && !sel[0].HasChildren)
            {
                return viewport.Flatten(((Entity)sel[0]).Origin);
            }
            var st = viewport.Flatten(state.PreTransformBoxStart ?? Vector3.Zero);
            var ed = viewport.Flatten(state.PreTransformBoxEnd ?? Vector3.Zero);
            var points = new[] { st, ed, new Vector3(st.X, ed.Y, 0), new Vector3(ed.X, st.Y, 0) };
            return points.OrderBy(x => ((state.MoveStart - x) ?? Vector3.Zero).LengthSquared()).First();
        }

        private Vector3? GetResizeDistance(Viewport2D viewport, ViewportEvent e, BaseBoxTool.BoxState state, Document document)
        {
            var origin = GetResizeOrigin(viewport, state, document);
            if (origin == null) { return null; }
            var before = state.MoveStart ?? Vector3.Zero;
            var after = viewport.ScreenToWorld(e.X, viewport.Height - e.Y);
            return SnapIfNeeded(origin.Value + after - before, document) - origin;
        }

        private (Vector3 Start, Vector3 End) GetBoxCoordinatesForSelectionResize(Viewport2D viewport, ViewportEvent e, BaseBoxTool.BoxState state, Document document)
        {
            if (state.Action != BaseBoxTool.BoxAction.Resizing) return (state.BoxStart ?? Vector3.Zero, state.BoxEnd ?? Vector3.Zero);
            var now = SnapIfNeeded(viewport.ScreenToWorld(e.X, viewport.Height - e.Y), document);
            var cstart = viewport.Flatten(state.BoxStart ?? Vector3.Zero);
            var cend = viewport.Flatten(state.BoxEnd ?? Vector3.Zero);

            // Proportional scaling
            var ostart = viewport.Flatten(state.PreTransformBoxStart ?? Vector3.Zero);
            var oend = viewport.Flatten(state.PreTransformBoxEnd ?? Vector3.Zero);
            var owidth = oend.X - ostart.X;
            var oheight = oend.Y - ostart.Y;
            var proportional = ViewportManager.Ctrl && state.Action == BaseBoxTool.BoxAction.Resizing && owidth != 0 && oheight != 0;

            switch (state.Handle)
            {
                case BaseBoxTool.ResizeHandle.TopLeft:
                    cstart.X = Math.Min(now.X, cend.X - 1);
                    cend.Y = Math.Max(now.Y, cstart.Y + 1);
                    break;
                case BaseBoxTool.ResizeHandle.Top:
                    cend.Y = Math.Max(now.Y, cstart.Y + 1);
                    break;
                case BaseBoxTool.ResizeHandle.TopRight:
                    cend.X = Math.Max(now.X, cstart.X + 1);
                    cend.Y = Math.Max(now.Y, cstart.Y + 1);
                    break;
                case BaseBoxTool.ResizeHandle.Left:
                    cstart.X = Math.Min(now.X, cend.X - 1);
                    break;
                case BaseBoxTool.ResizeHandle.Center:
                    var cdiff = cend - cstart;

                    var distance = GetResizeDistance(viewport, e, state, document);
                    if (distance == null) cstart = viewport.Flatten(state.PreTransformBoxStart ?? Vector3.Zero) + now - SnapIfNeeded(state.MoveStart ?? Vector3.Zero, document);
                    else cstart = viewport.Flatten(state.PreTransformBoxStart ?? Vector3.Zero) + distance.Value;
                    cend = cstart + cdiff;
                    break;
                case BaseBoxTool.ResizeHandle.Right:
                    cend.X = Math.Max(now.X, cstart.X + 1);
                    break;
                case BaseBoxTool.ResizeHandle.BottomLeft:
                    cstart.X = Math.Min(now.X, cend.X - 1);
                    cstart.Y = Math.Min(now.Y, cend.Y - 1);
                    break;
                case BaseBoxTool.ResizeHandle.Bottom:
                    cstart.Y = Math.Min(now.Y, cend.Y - 1);
                    break;
                case BaseBoxTool.ResizeHandle.BottomRight:
                    cend.X = Math.Max(now.X, cstart.X + 1);
                    cstart.Y = Math.Min(now.Y, cend.Y - 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (proportional)
            {
                var nwidth = cend.X - cstart.X;
                var nheight = cend.Y - cstart.Y;
                var mult = Math.Max(nwidth / owidth, nheight / oheight);
                var pwidth = owidth * mult;
                var pheight = oheight * mult;
                var wdiff = pwidth - nwidth;
                var hdiff = pheight - nheight;
                switch (state.Handle)
                {
                    case BaseBoxTool.ResizeHandle.TopLeft:
                        cstart.X -= wdiff;
                        cend.Y += hdiff;
                        break;
                    case BaseBoxTool.ResizeHandle.TopRight:
                        cend.X += wdiff;
                        cend.Y += hdiff;
                        break;
                    case BaseBoxTool.ResizeHandle.BottomLeft:
                        cstart.X -= wdiff;
                        cstart.Y -= hdiff;
                        break;
                    case BaseBoxTool.ResizeHandle.BottomRight:
                        cend.X += wdiff;
                        cstart.Y -= hdiff;
                        break;
                }
            }

            cstart = viewport.Expand(cstart) + viewport.GetUnusedCoordinate(state.BoxStart ?? Vector3.Zero);
            cend = viewport.Expand(cend) + viewport.GetUnusedCoordinate(state.BoxEnd ?? Vector3.Zero);
            return (cstart, cend);
        }

        private static Vector3 GetOriginForTransform(Viewport2D viewport, BaseBoxTool.BoxState state)
        {
            decimal x = 0;
            decimal y = 0;
            var cstart = viewport.Flatten(state.PreTransformBoxStart ?? Vector3.Zero);
            var cend = viewport.Flatten(state.PreTransformBoxEnd ?? Vector3.Zero);
            switch (state.Handle)
            {
                case BaseBoxTool.ResizeHandle.TopLeft:
                case BaseBoxTool.ResizeHandle.Top:
                case BaseBoxTool.ResizeHandle.TopRight:
                case BaseBoxTool.ResizeHandle.Left:
                case BaseBoxTool.ResizeHandle.Right:
                    y = cstart.Y;
                    break;
                case BaseBoxTool.ResizeHandle.BottomLeft:
                case BaseBoxTool.ResizeHandle.Bottom:
                case BaseBoxTool.ResizeHandle.BottomRight:
                    y = cend.Y;
                    break;
            }
            switch (state.Handle)
            {
                case BaseBoxTool.ResizeHandle.Top:
                case BaseBoxTool.ResizeHandle.TopRight:
                case BaseBoxTool.ResizeHandle.Right:
                case BaseBoxTool.ResizeHandle.BottomRight:
                case BaseBoxTool.ResizeHandle.Bottom:
                    x = cstart.X;
                    break;
                case BaseBoxTool.ResizeHandle.TopLeft:
                case BaseBoxTool.ResizeHandle.Left:
                case BaseBoxTool.ResizeHandle.BottomLeft:
                    x = cend.X;
                    break;
            }
            return viewport.Expand(new Vector3(x, y, 0));
        }
        #endregion 2D Transformation Matrix

        public override IEnumerable<Widget> GetWidgets(Document document)
        {
            yield break;
        }
    }
}

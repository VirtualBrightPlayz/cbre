using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using CBRE.Graphics;

namespace CBRE.Editor.Tools
{
    public class ClipTool : BaseTool
    {
        public enum ClipState
        {
            None,
            Drawing,
            Drawn,
            MovingPoint1,
            MovingPoint2,
            MovingPoint3
        }

        public enum ClipSide
        {
            Both,
            Front,
            Back
        }

        private Vector3? _clipPlanePoint1;
        private Vector3? _clipPlanePoint2;
        private Vector3? _clipPlanePoint3;
        private Vector3? _drawingPoint;
        private ClipState _prevState;
        private ClipState _state;
        private ClipSide _side;

        public ClipTool()
        {
            Usage = ToolUsage.Both;
            _clipPlanePoint1 = _clipPlanePoint2 = _clipPlanePoint3 = _drawingPoint = null;
            _state = _prevState = ClipState.None;
            _side = ClipSide.Both;
        }

        public override string GetIcon()
        {
            return "Tool_Clip";
        }

        public override string GetName()
        {
            return "Clip Tool";
        }

        public override HotkeyTool? GetHotkeyToolType()
        {
            return HotkeyTool.Clip;
        }

        public override string GetContextualHelp()
        {
            return "*Click* and drag to define the clipping plane.\n" +
                   "*Click* and drag any of the three points to change the orientation of the plane.\n" +
                   "Press *enter* to cut the selected solids along the clipping plane.";
        }

        private ClipState GetStateAtPoint(int x, int y, Viewport2D viewport)
        {
            if (_clipPlanePoint1 == null || _clipPlanePoint2 == null || _clipPlanePoint3 == null) return ClipState.None;

            var p = viewport.ScreenToWorld(x, y);
            var p1 = viewport.Flatten(_clipPlanePoint1 ?? Vector3.Zero);
            var p2 = viewport.Flatten(_clipPlanePoint2 ?? Vector3.Zero);
            var p3 = viewport.Flatten(_clipPlanePoint3 ?? Vector3.Zero);

            var d = 5 / viewport.Zoom;

            if (p.X >= p1.X - d && p.X <= p1.X + d && p.Y >= p1.Y - d && p.Y <= p1.Y + d) return ClipState.MovingPoint1;
            if (p.X >= p2.X - d && p.X <= p2.X + d && p.Y >= p2.Y - d && p.Y <= p2.Y + d) return ClipState.MovingPoint2;
            if (p.X >= p3.X - d && p.X <= p3.X + d && p.Y >= p3.Y - d && p.Y <= p3.Y + d) return ClipState.MovingPoint3;

            return ClipState.None;
        }

        public override void MouseClick(ViewportBase vp, ViewportEvent e)
        {
            if (!(vp is Viewport2D)) return;
            var viewport = (Viewport2D)vp;
            _prevState = _state;

            var point = SnapIfNeeded(viewport.Expand(viewport.ScreenToWorld(e.X, viewport.Height - e.Y)));
            var st = GetStateAtPoint(e.X, viewport.Height - e.Y, viewport);
            if (_state == ClipState.None || st == ClipState.None)
            {
                _state = ClipState.Drawing;
                _drawingPoint = point;
            }
            else if (_state == ClipState.Drawn)
            {
                _state = st;
            }
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
        }

        public override void MouseLifted(ViewportBase vp, ViewportEvent e)
        {
            if (!(vp is Viewport2D)) return;
            var viewport = (Viewport2D)vp;

            var point = SnapIfNeeded(viewport.Expand(viewport.ScreenToWorld(e.X, viewport.Height - e.Y)));
            if (_state == ClipState.Drawing)
            {
                // Do nothing
                _state = _prevState;
            }
            else
            {
                _state = ClipState.Drawn;
            }

            //Editor.Instance.CaptureAltPresses = false;
        }

        public override void MouseMove(ViewportBase vp, ViewportEvent e)
        {
            if (!(vp is Viewport2D)) return;
            var viewport = (Viewport2D)vp;

            var point = SnapIfNeeded(viewport.Expand(viewport.ScreenToWorld(e.X, viewport.Height - e.Y)));
            var st = GetStateAtPoint(e.X, viewport.Height - e.Y, viewport);
            if (_state == ClipState.Drawing)
            {
                _state = ClipState.MovingPoint2;
                _clipPlanePoint1 = _drawingPoint;
                _clipPlanePoint2 = point;
                _clipPlanePoint3 = _clipPlanePoint1 + SnapIfNeeded(viewport.GetUnusedCoordinate(new Vector3(128, 128, 128)));
            }
            else if (_state == ClipState.MovingPoint1)
            {
                // Move point 1
                var cp1 = viewport.GetUnusedCoordinate(_clipPlanePoint1 ?? Vector3.Zero) + point;
                if (ViewportManager.Ctrl)
                {
                    var diff = _clipPlanePoint1 - cp1;
                    _clipPlanePoint2 -= diff;
                    _clipPlanePoint3 -= diff;
                }
                _clipPlanePoint1 = cp1;
            }
            else if (_state == ClipState.MovingPoint2)
            {
                // Move point 2
                var cp2 = viewport.GetUnusedCoordinate(_clipPlanePoint2 ?? Vector3.Zero) + point;
                if (ViewportManager.Ctrl)
                {
                    var diff = _clipPlanePoint2 - cp2;
                    _clipPlanePoint1 -= diff;
                    _clipPlanePoint3 -= diff;
                }
                _clipPlanePoint2 = cp2;
            }
            else if (_state == ClipState.MovingPoint3)
            {
                // Move point 3
                var cp3 = viewport.GetUnusedCoordinate(_clipPlanePoint3 ?? Vector3.Zero) + point;
                if (ViewportManager.Ctrl)
                {
                    var diff = _clipPlanePoint3 - cp3;
                    _clipPlanePoint1 -= diff;
                    _clipPlanePoint2 -= diff;
                }
                _clipPlanePoint3 = cp3;
            }

            //Editor.Instance.CaptureAltPresses = _state != ClipState.None && _state != ClipState.Drawn;

            if (st != ClipState.None || (_state != ClipState.None && _state != ClipState.Drawn))
            {
                viewport.Cursor = MouseCursor.Crosshair;
            }
            else
            {
                viewport.Cursor = MouseCursor.Arrow;
            }
        }

        public override void KeyHit(ViewportBase viewport, ViewportEvent e) {
            if (_clipPlanePoint1 is not { } clipPlanePoint1
                || _clipPlanePoint2 is not { } clipPlanePoint2
                || _clipPlanePoint3 is not { } clipPlanePoint3) { return; }
            
            if (e.KeyCode == Keys.Enter) // Enter
            {
                if (!clipPlanePoint1.EquivalentTo(clipPlanePoint2)
                    && !clipPlanePoint2.EquivalentTo(clipPlanePoint3)
                    && !clipPlanePoint1.EquivalentTo(clipPlanePoint3)) // Don't clip if the points are too close together
                {
                    PerformClip();
                }
            }
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) // Escape cancels, Enter commits and resets
            {
                _clipPlanePoint1 = _clipPlanePoint2 = _clipPlanePoint3 = _drawingPoint = null;
                _state = _prevState = ClipState.None;
            }
        }

        private void PerformClip()
        {
            if (_clipPlanePoint1 is not { } clipPlanePoint1
                || _clipPlanePoint2 is not { } clipPlanePoint2
                || _clipPlanePoint3 is not { } clipPlanePoint3) { return; }
            
            var objects = Document.Selection.GetSelectedObjects().OfType<Solid>().ToList();
            var plane = new Plane(clipPlanePoint1, clipPlanePoint2, clipPlanePoint3);
            Document.PerformAction("Perform Clip", new Clip(objects, plane, _side != ClipSide.Back, _side != ClipSide.Front));
        }

        public override void Render(ViewportBase viewport)
        {
            if (viewport is Viewport2D) Render2D((Viewport2D)viewport);
            if (viewport is Viewport3D) Render3D((Viewport3D)viewport);
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters)
        {
            switch (hotkeyMessage)
            {
                case HotkeysMediator.OperationsPasteSpecial:
                case HotkeysMediator.OperationsPaste:
                    return HotkeyInterceptResult.SwitchToSelectTool;
                case HotkeysMediator.SwitchTool:
                    if (parameters is HotkeyTool && (HotkeyTool)parameters == GetHotkeyToolType())
                    {
                        CycleClipSide();
                        return HotkeyInterceptResult.Abort;
                    }
                    break;
            }
            return HotkeyInterceptResult.Continue;
        }

        private void CycleClipSide()
        {
            var side = (int)_side;
            side = (side + 1) % (Enum.GetValues(typeof(ClipSide)).Length);
            _side = (ClipSide)side;
        }

        private void Render2D(Viewport2D vp)
        {
            if (_state == ClipState.None
                || _clipPlanePoint1 is not { } clipPlanePoint1
                || _clipPlanePoint2 is not { } clipPlanePoint2
                || _clipPlanePoint3 is not { } clipPlanePoint3) { return; }  // Nothing to draw at this point

            var z = (double)vp.Zoom;
            var p1 = vp.Flatten(clipPlanePoint1);
            var p2 = vp.Flatten(clipPlanePoint2);
            var p3 = vp.Flatten(clipPlanePoint3);

            // Draw points
            PrimitiveDrawing.Begin(PrimitiveType.QuadList);
            PrimitiveDrawing.SetColor(Color.White);
            PrimitiveDrawing.Square(new Vector3(p1.X, p1.Y, (decimal)z), 4m / vp.Zoom);
            PrimitiveDrawing.Square(new Vector3(p2.X, p2.Y, (decimal)z), 4m / vp.Zoom);
            PrimitiveDrawing.Square(new Vector3(p3.X, p3.Y, (decimal)z), 4m / vp.Zoom);
            PrimitiveDrawing.End();

            // Draw lines between points and point outlines
            PrimitiveDrawing.Begin(PrimitiveType.LineList);
            PrimitiveDrawing.SetColor(Color.White);
            PrimitiveDrawing.Vertex2(p1.DX, p1.DY);
            PrimitiveDrawing.Vertex2(p2.DX, p2.DY);
            PrimitiveDrawing.Vertex2(p2.DX, p2.DY);
            PrimitiveDrawing.Vertex2(p3.DX, p3.DY);
            PrimitiveDrawing.Vertex2(p3.DX, p3.DY);
            PrimitiveDrawing.Vertex2(p1.DX, p1.DY);
            PrimitiveDrawing.SetColor(Color.Black);
            PrimitiveDrawing.Square(new Vector3(p1.X, p1.Y, (decimal)z), 4m / vp.Zoom);
            PrimitiveDrawing.Square(new Vector3(p2.X, p2.Y, (decimal)z), 4m / vp.Zoom);
            PrimitiveDrawing.Square(new Vector3(p3.X, p3.Y, (decimal)z), 4m / vp.Zoom);
            PrimitiveDrawing.End();

            // Draw the clipped brushes
            if (!clipPlanePoint1.EquivalentTo(clipPlanePoint2)
                    && !clipPlanePoint2.EquivalentTo(clipPlanePoint3)
                    && !clipPlanePoint1.EquivalentTo(clipPlanePoint3))
            {
                var plane = new Plane(clipPlanePoint1, clipPlanePoint2, clipPlanePoint3);
                var faces = new List<Face>();
                var idg = new IDGenerator();
                foreach (var solid in Document.Selection.GetSelectedObjects().OfType<Solid>().ToList())
                {
                    Solid back, front;
                    if (solid.Split(plane, out back, out front, idg))
                    {
                        if (_side != ClipSide.Front) faces.AddRange(back.Faces);
                        if (_side != ClipSide.Back) faces.AddRange(front.Faces);
                    }
                }
                PrimitiveDrawing.Begin(PrimitiveType.TriangleList);
                PrimitiveDrawing.SetColor(Color.White);
                var mat = vp.GetModelViewMatrix();
                PrimitiveDrawing.FacesWireframe(faces, thickness: 1.0m / vp.Zoom, m: mat.ToCbre());
                PrimitiveDrawing.End();
            }
        }

        private void Render3D(Viewport3D vp)
        {
            if (_state == ClipState.None
                || _clipPlanePoint1 is not { } clipPlanePoint1
                || _clipPlanePoint2 is not { } clipPlanePoint2
                || _clipPlanePoint3 is not { } clipPlanePoint3
                || Document.Selection.IsEmpty()) return; // Nothing to draw at this point

            // Draw points

            if (!clipPlanePoint1.EquivalentTo(clipPlanePoint2)
                    && !clipPlanePoint2.EquivalentTo(clipPlanePoint3)
                    && !clipPlanePoint1.EquivalentTo(clipPlanePoint3))
            {
                var plane = new Plane(clipPlanePoint1, clipPlanePoint2, clipPlanePoint3);

                // Draw clipped solids
                var faces = new List<Face>();
                var idg = new IDGenerator();
                foreach (var solid in Document.Selection.GetSelectedObjects().OfType<Solid>().ToList())
                {
                    if (solid.Split(plane, out var back, out var front, idg))
                    {
                        if (_side != ClipSide.Front) faces.AddRange(back.Faces);
                        if (_side != ClipSide.Back) faces.AddRange(front.Faces);
                    }
                }
                PrimitiveDrawing.Begin(PrimitiveType.TriangleList);
                PrimitiveDrawing.SetColor(Color.White);
                PrimitiveDrawing.FacesWireframe(faces, thickness: 2.0f);
                PrimitiveDrawing.End();

                // Draw the clipping plane
                var poly = new Polygon(plane);
                var bbox = Document.Selection.GetSelectionBoundingBox();
                var point = bbox.Center;
                foreach (var boxPlane in bbox.GetBoxPlanes())
                {
                    var proj = boxPlane.Project(point);
                    var dist = (point - proj).VectorMagnitude() * 0.1m;
                    poly.Split(new Plane(boxPlane.Normal, proj + boxPlane.Normal * Math.Max(dist, 100)));
                }

                PrimitiveDrawing.Begin(PrimitiveType.TriangleFan);
                PrimitiveDrawing.SetColor(Color.FromArgb(100, Color.Turquoise));
                foreach (var c in poly.Vertices)  { PrimitiveDrawing.Vertex3(c.DX, c.DY, c.DZ); }
                foreach (var c in Enumerable.Reverse(poly.Vertices))  { PrimitiveDrawing.Vertex3(c.DX, c.DY, c.DZ); }
                PrimitiveDrawing.End();
            }
        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e)
        {
            //
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e)
        {
            //
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e)
        {
            //
        }

        public override void KeyLift(ViewportBase viewport, ViewportEvent e)
        {
            //
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame)
        {
            //
        }
    }
}

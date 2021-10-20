using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Drawing;
using System.Linq;
using Num = System.Numerics;

#pragma warning disable 0612
namespace CBRE.Editor.Tools
{
    public abstract class BaseBoxTool : BaseTool
    {
        // Enum/Class Declarations
        public enum BoxAction
        {
            ReadyToDraw,
            DownToDraw,
            Drawing,
            Drawn,
            ReadyToResize,
            DownToResize,
            Resizing
        }

        public enum ResizeHandle
        {
            TopLeft, Top, TopRight,
            Left, Center, Right,
            BottomLeft, Bottom, BottomRight
        }

        public class BoxState
        {
            public BoxState()
            {
                ActiveViewport = null;
                Action = BoxAction.ReadyToDraw;
                Handle = ResizeHandle.Center;
                BoxStart = null;
                BoxEnd = null;
                MoveStart = null;
                PreTransformBoxStart = null;
                PreTransformBoxEnd = null;
                ClickStart = new Point(0, 0);
            }

            public bool IsValidAndApplicable(ViewportBase vp)
            {
                return (Action != BoxAction.DownToDraw
                        && Action != BoxAction.Drawing
                        && Action != BoxAction.DownToResize
                        && Action != BoxAction.Resizing)
                        || ActiveViewport == vp;
            }

            public void FixBoxBounds()
            {
                if (Action != BoxAction.Drawing && Action != BoxAction.Resizing) return;
                if (!(ActiveViewport is Viewport2D)) return;
                var vp = (Viewport2D)ActiveViewport;

                if (BoxStart.X > BoxEnd.X)
                {
                    var temp = BoxStart.X;
                    BoxStart.X = BoxEnd.X;
                    BoxEnd.X = temp;
                    var flat = vp.Flatten(Vector3.UnitX);
                    if (flat.X == 1) SwapHandle("Left", "Right");
                    if (flat.Y == 1) SwapHandle("Top", "Bottom");
                }
                if (BoxStart.Y > BoxEnd.Y)
                {
                    var temp = BoxStart.Y;
                    BoxStart.Y = BoxEnd.Y;
                    BoxEnd.Y = temp;
                    var flat = vp.Flatten(Vector3.UnitY);
                    if (flat.X == 1) SwapHandle("Left", "Right");
                    if (flat.Y == 1) SwapHandle("Top", "Bottom");
                }
                if (BoxStart.Z > BoxEnd.Z)
                {
                    var temp = BoxStart.Z;
                    BoxStart.Z = BoxEnd.Z;
                    BoxEnd.Z = temp;
                    var flat = vp.Flatten(Vector3.UnitZ);
                    if (flat.X == 1) SwapHandle("Left", "Right");
                    if (flat.Y == 1) SwapHandle("Top", "Bottom");
                }

                ViewportManager.MarkForRerender();
            }

            public void SwapHandle(string one, string two)
            {
                var str = Handle.ToString();
                if (str.Contains(one))
                {
                    Handle = (ResizeHandle)Enum.Parse(typeof(ResizeHandle), str.Replace(one, two));
                }
                else if (str.Contains(two))
                {
                    Handle = (ResizeHandle)Enum.Parse(typeof(ResizeHandle), str.Replace(two, one));
                }
            }

            public ViewportBase ActiveViewport { get; set; }
            public BoxAction Action { get; set; }
            public ResizeHandle Handle { get; set; }
            public Vector3 BoxStart { get; set; }
            public Vector3 BoxEnd { get; set; }
            public Vector3 MoveStart { get; set; }
            public Vector3 PreTransformBoxStart { get; set; }
            public Vector3 PreTransformBoxEnd { get; set; }
            public Point ClickStart { get; set; }
        }

        // Static Methods
        protected static Tuple<Vector3, Vector3> GetProperBoxCoordinates(Vector3 start, Vector3 end)
        {
            var newStart = new Vector3(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
            var newEnd = new Vector3(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y), Math.Max(start.Z, end.Z));
            return Tuple.Create(newStart, newEnd);
        }

        private static bool HandleHitTestPoint(decimal hitX, decimal hitY, decimal testX, decimal testY, decimal hitbox)
        {
            return (hitX >= testX - hitbox && hitX <= testX + hitbox && hitY >= testY - hitbox && hitY <= testY + hitbox);
        }

        private static bool HandleHitTestLine(decimal hitX, decimal hitY, decimal test1X, decimal test1Y, decimal test2X, decimal test2Y, decimal hitbox)
        {
            if (test1X != test2X && test1Y != test2Y) return false; // Only works on straight lines
            var sx = Math.Min(test1X, test2X);
            var sy = Math.Min(test1Y, test2Y);
            var ex = Math.Max(test1X, test2X);
            var ey = Math.Max(test1Y, test2Y);
            var hoz = test1Y == test2Y;
            return hoz
                       ? hitX >= sx && hitX <= ex && hitY >= sy - hitbox && hitY <= sy + hitbox
                       : hitY >= sy && hitY <= ey && hitX >= sx - hitbox && hitX <= sx + hitbox;
        }

        protected static ResizeHandle? GetHandle(Vector3 current, Vector3 boxStart, Vector3 boxEnd, decimal hitbox)
        {
            var start = new Vector3(Math.Min(boxStart.X, boxEnd.X), Math.Min(boxStart.Y, boxEnd.Y), 0);
            var end = new Vector3(Math.Max(boxStart.X, boxEnd.X), Math.Max(boxStart.Y, boxEnd.Y), 0);
            if (HandleHitTestPoint(current.X, current.Y, start.X, start.Y, hitbox)) return ResizeHandle.BottomLeft;
            if (HandleHitTestPoint(current.X, current.Y, end.X, start.Y, hitbox)) return ResizeHandle.BottomRight;
            if (HandleHitTestPoint(current.X, current.Y, start.X, end.Y, hitbox)) return ResizeHandle.TopLeft;
            if (HandleHitTestPoint(current.X, current.Y, end.X, end.Y, hitbox)) return ResizeHandle.TopRight;
            if (HandleHitTestLine(current.X, current.Y, start.X, start.Y, end.X, start.Y, hitbox)) return ResizeHandle.Bottom;
            if (HandleHitTestLine(current.X, current.Y, start.X, end.Y, end.X, end.Y, hitbox)) return ResizeHandle.Top;
            if (HandleHitTestLine(current.X, current.Y, start.X, start.Y, start.X, end.Y, hitbox)) return ResizeHandle.Left;
            if (HandleHitTestLine(current.X, current.Y, end.X, start.Y, end.X, end.Y, hitbox)) return ResizeHandle.Right;
            if (current.X > start.X && current.X < end.X && current.Y > start.Y && current.Y < end.Y) return ResizeHandle.Center;
            return null;
        }

        // Class Variables
        protected const decimal HandleWidth = 12;

        protected abstract Color BoxColour { get; }
        protected abstract Color FillColour { get; }
        internal BoxState State { get; set; }

        protected BaseBoxTool()
        {
            Usage = ToolUsage.Both;
            State = new BoxState();
        }

        protected virtual void OnBoxChanged()
        {
            State.FixBoxBounds();
            Mediator.Publish(EditorMediator.SelectionBoxChanged,
                             State.Action == BoxAction.ReadyToDraw
                                 ? Box.Empty
                                 : new Box(State.BoxStart, State.BoxEnd));
        }

        // Mouse Down
        public override void MouseDown(ViewportBase viewport, ViewportEvent e)
        {
            if (viewport is Viewport3D) MouseDown3D((Viewport3D)viewport, e);
            if (e.Button != MouseButtons.Left) return;
            if (!(viewport is Viewport2D)) return;
            State.ClickStart = new Point(e.X, e.Y);
            var vp = (Viewport2D)viewport;
            switch (State.Action)
            {
                case BoxAction.ReadyToDraw:
                case BoxAction.Drawn:
                    LeftMouseDownToDraw(vp, e);
                    break;
                case BoxAction.ReadyToResize:
                    LeftMouseDownToResize(vp, e);
                    break;
            }
        }

        protected virtual void MouseDown3D(Viewport3D viewport, ViewportEvent e)
        {
            // Virtual
        }

        protected virtual void LeftMouseDownToDraw(Viewport2D viewport, ViewportEvent e)
        {
            State.ActiveViewport = viewport;
            State.Action = BoxAction.DownToDraw;
            State.BoxStart = SnapIfNeeded(viewport.Expand(viewport.ScreenToWorld(e.X, viewport.Height - e.Y)));
            State.BoxEnd = State.BoxStart;
            State.Handle = ResizeHandle.BottomLeft;
            OnBoxChanged();
        }

        protected virtual void LeftMouseDownToResize(Viewport2D viewport, ViewportEvent e)
        {
            State.ActiveViewport = viewport;
            State.Action = BoxAction.DownToResize;
            State.MoveStart = viewport.ScreenToWorld(e.X, viewport.Height - e.Y);
            State.PreTransformBoxStart = State.BoxStart;
            State.PreTransformBoxEnd = State.BoxEnd;
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
        }

        // Mouse Up
        public override void MouseUp(ViewportBase viewport, ViewportEvent e)
        {
            if (viewport is Viewport3D) MouseUp3D((Viewport3D)viewport, e);
            //Editor.Instance.CaptureAltPresses = false;
            if (e.Button != MouseButtons.Left) return;
            if (!(viewport is Viewport2D)) return;
            var vp = (Viewport2D)viewport;
            switch (State.Action)
            {
                case BoxAction.Drawing:
                    LeftMouseUpDrawing(vp, e);
                    break;
                case BoxAction.Resizing:
                    LeftMouseUpResizing(vp, e);
                    break;
                case BoxAction.DownToDraw:
                    LeftMouseClick(vp, e);
                    break;
                case BoxAction.DownToResize:
                    LeftMouseClickOnResizeHandle(vp, e);
                    break;
            }
        }

        protected virtual void MouseUp3D(Viewport3D viewport, ViewportEvent e)
        {
            // Virtual
        }

        protected virtual void LeftMouseUpDrawing(Viewport2D viewport, ViewportEvent e)
        {
            var coords = GetResizedBoxCoordinates(viewport, e);
            var corrected = GetProperBoxCoordinates(coords.Item1, coords.Item2);
            State.ActiveViewport = null;
            State.Action = BoxAction.Drawn;
            State.BoxStart = corrected.Item1;
            State.BoxEnd = corrected.Item2;
            OnBoxChanged();
        }

        protected virtual void LeftMouseUpResizing(Viewport2D viewport, ViewportEvent e)
        {
            var coords = GetResizedBoxCoordinates(viewport, e);
            var corrected = GetProperBoxCoordinates(coords.Item1, coords.Item2);
            State.ActiveViewport = null;
            State.Action = BoxAction.Drawn;
            State.BoxStart = corrected.Item1;
            State.BoxEnd = corrected.Item2;
            OnBoxChanged();
        }

        protected virtual void LeftMouseClick(Viewport2D viewport, ViewportEvent e)
        {
            State.ActiveViewport = null;
            State.Action = BoxAction.ReadyToDraw;
            State.BoxStart = null;
            State.BoxEnd = null;
            OnBoxChanged();
        }

        protected virtual void LeftMouseClickOnResizeHandle(Viewport2D vp, ViewportEvent e)
        {
            State.Action = BoxAction.ReadyToResize;
        }

        // Mouse Move
        public override void MouseMove(ViewportBase viewport, ViewportEvent e)
        {
            if (viewport is Viewport3D) MouseMove3D((Viewport3D)viewport, e);
            if (!(viewport is Viewport2D)) return;
            if (!State.IsValidAndApplicable(viewport)) return;
            if (Math.Abs(e.X - State.ClickStart.X) <= 2 && Math.Abs(e.Y - State.ClickStart.Y) <= 2) return;
            var vp = (Viewport2D)viewport;
            switch (State.Action)
            {
                case BoxAction.Drawing:
                case BoxAction.DownToDraw:
                    //Editor.Instance.CaptureAltPresses = true;
                    MouseDraggingToDraw(vp, e);
                    break;
                case BoxAction.Drawn:
                case BoxAction.ReadyToResize:
                    MouseHoverWhenDrawn(vp, e);
                    break;
                case BoxAction.DownToResize:
                case BoxAction.Resizing:
                    //Editor.Instance.CaptureAltPresses = true;
                    MouseDraggingToResize(vp, e);
                    break;
            }
        }

        protected virtual void MouseMove3D(Viewport3D viewport, ViewportEvent e)
        {
            // Virtual
        }

        protected virtual void MouseDraggingToDraw(Viewport2D viewport, ViewportEvent e)
        {
            State.Action = BoxAction.Drawing;
            var coords = GetResizedBoxCoordinates(viewport, e);
            State.BoxStart = coords.Item1;
            State.BoxEnd = coords.Item2;
            OnBoxChanged();
        }

        protected virtual void MouseDraggingToResize(Viewport2D viewport, ViewportEvent e)
        {
            State.Action = BoxAction.Resizing;
            var coords = GetResizedBoxCoordinates(viewport, e);
            State.BoxStart = coords.Item1;
            State.BoxEnd = coords.Item2;
            OnBoxChanged();
        }

        protected virtual MouseCursor CursorForHandle(ResizeHandle handle)
        {
            switch (handle)
            {
                case ResizeHandle.TopLeft:
                case ResizeHandle.BottomRight:
                    return MouseCursor.SizeNWSE;
                case ResizeHandle.TopRight:
                case ResizeHandle.BottomLeft:
                    return MouseCursor.SizeNESW;
                case ResizeHandle.Top:
                case ResizeHandle.Bottom:
                    return MouseCursor.SizeNS;
                case ResizeHandle.Left:
                case ResizeHandle.Right:
                    return MouseCursor.SizeWE;
                case ResizeHandle.Center:
                    return MouseCursor.SizeAll;
                default:
                    return MouseCursor.Arrow;
            }
        }

        protected virtual void MouseHoverWhenDrawn(Viewport2D viewport, ViewportEvent e)
        {
            var now = viewport.ScreenToWorld(e.X, viewport.Height - e.Y);
            var start = viewport.Flatten(State.BoxStart);
            var end = viewport.Flatten(State.BoxEnd);
            var handle = GetHandle(now, start, end, HandleWidth / viewport.Zoom);
            if (handle.HasValue)
            {
                viewport.Cursor = CursorForHandle(handle.Value);
                State.Handle = handle.Value;
                State.Action = BoxAction.ReadyToResize;
                State.ActiveViewport = viewport;
            }
            else
            {
                viewport.Cursor = MouseCursor.Arrow;
                State.Action = BoxAction.Drawn;
                State.ActiveViewport = null;
            }
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e)
        {
            // Nope.
        }

        protected virtual Vector3 GetResizeOrigin(Viewport2D viewport)
        {
            if (State.Action != BoxAction.Resizing || State.Handle != ResizeHandle.Center) return null;
            var st = viewport.Flatten(State.PreTransformBoxStart);
            var ed = viewport.Flatten(State.PreTransformBoxEnd);
            var points = new[] { st, ed, new Vector3(st.X, ed.Y, 0), new Vector3(ed.X, st.Y, 0) };
            return points.OrderBy(x => (State.MoveStart - x).LengthSquared()).First();
        }

        protected virtual Vector3 GetResizeDistance(Viewport2D viewport, ViewportEvent e)
        {
            var origin = GetResizeOrigin(viewport);
            if (origin == null) return null;
            var before = State.MoveStart;
            var after = viewport.ScreenToWorld(e.X, viewport.Height - e.Y);
            return SnapIfNeeded(origin + after - before) - origin;
        }

        protected Tuple<Vector3, Vector3> GetResizedBoxCoordinates(Viewport2D viewport, ViewportEvent e)
        {
            if (State.Action != BoxAction.Resizing && State.Action != BoxAction.Drawing) return Tuple.Create(State.BoxStart, State.BoxEnd);
            var now = SnapIfNeeded(viewport.ScreenToWorld(e.X, viewport.Height - e.Y));
            var cstart = viewport.Flatten(State.BoxStart);
            var cend = viewport.Flatten(State.BoxEnd);

            // Proportional scaling
            var ostart = viewport.Flatten(State.PreTransformBoxStart ?? Vector3.Zero);
            var oend = viewport.Flatten(State.PreTransformBoxEnd ?? Vector3.Zero);
            var owidth = oend.X - ostart.X;
            var oheight = oend.Y - ostart.Y;
            var proportional = ViewportManager.Ctrl && State.Action == BoxAction.Resizing && owidth != 0 && oheight != 0;

            switch (State.Handle)
            {
                case ResizeHandle.TopLeft:
                    cstart.X = now.X;
                    cend.Y = now.Y;
                    break;
                case ResizeHandle.Top:
                    cend.Y = now.Y;
                    break;
                case ResizeHandle.TopRight:
                    cend.X = now.X;
                    cend.Y = now.Y;
                    break;
                case ResizeHandle.Left:
                    cstart.X = now.X;
                    break;
                case ResizeHandle.Center:
                    var cdiff = cend - cstart;
                    var distance = GetResizeDistance(viewport, e);
                    if (distance == null) cstart = viewport.Flatten(State.PreTransformBoxStart) + now - SnapIfNeeded(State.MoveStart);
                    else cstart = viewport.Flatten(State.PreTransformBoxStart) + distance;
                    cend = cstart + cdiff;
                    break;
                case ResizeHandle.Right:
                    cend.X = now.X;
                    break;
                case ResizeHandle.BottomLeft:
                    cstart.X = now.X;
                    cstart.Y = now.Y;
                    break;
                case ResizeHandle.Bottom:
                    cstart.Y = now.Y;
                    break;
                case ResizeHandle.BottomRight:
                    cend.X = now.X;
                    cstart.Y = now.Y;
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
                switch (State.Handle)
                {
                    case ResizeHandle.TopLeft:
                        cstart.X -= wdiff;
                        cend.Y += hdiff;
                        break;
                    case ResizeHandle.TopRight:
                        cend.X += wdiff;
                        cend.Y += hdiff;
                        break;
                    case ResizeHandle.BottomLeft:
                        cstart.X -= wdiff;
                        cstart.Y -= hdiff;
                        break;
                    case ResizeHandle.BottomRight:
                        cend.X += wdiff;
                        cstart.Y -= hdiff;
                        break;
                }
            }
            cstart = viewport.Expand(cstart) + viewport.GetUnusedCoordinate(State.BoxStart);
            cend = viewport.Expand(cend) + viewport.GetUnusedCoordinate(State.BoxEnd);
            return Tuple.Create(cstart, cend);
        }

        public override void KeyPress(ViewportBase viewport, ViewportEvent e)
        {

        }

        public override void KeyDown(ViewportBase viewport, ViewportEvent e)
        {
            if (State.Action == BoxAction.ReadyToDraw || State.Action == BoxAction.DownToDraw) return;
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    BoxDrawnConfirm(viewport);
                    break;
                case Keys.Escape:
                    BoxDrawnCancel(viewport);
                    break;
            }
        }

        public virtual void BoxDrawnConfirm(ViewportBase viewport)
        {
            if (State.ActiveViewport != null) State.ActiveViewport.Cursor = MouseCursor.Arrow;
            State.Action = BoxAction.ReadyToDraw;
            State.ActiveViewport = null;
        }

        public virtual void BoxDrawnCancel(ViewportBase viewport)
        {
            // throw new NotImplementedException();
            if (State.ActiveViewport != null) State.ActiveViewport.Cursor = MouseCursor.Arrow;
            State.Action = BoxAction.ReadyToDraw;
            State.ActiveViewport = null;
        }

        public override void KeyUp(ViewportBase viewport, ViewportEvent e)
        {
            // Probably not needed
        }

        #region Rendering
        public override void Render(ViewportBase viewport)
        {
            if (viewport is Viewport2D) Render2D((Viewport2D)viewport);
            if (viewport is Viewport3D) Render3D((Viewport3D)viewport);
        }

        protected virtual bool ShouldDrawBox(ViewportBase viewport)
        {
            return State.Action == BoxAction.Drawing
                   || State.Action == BoxAction.Drawn
                   || State.Action == BoxAction.ReadyToResize
                   || State.Action == BoxAction.DownToResize
                   || State.Action == BoxAction.Resizing;
        }

        protected virtual bool ShouldDrawBoxText(ViewportBase viewport)
        {
            return ShouldDrawBox(viewport);
        }

        protected virtual Color GetRenderFillColour()
        {
            return FillColour;
        }

        protected virtual Color GetRenderBoxColour()
        {
            return BoxColour;
        }

        protected virtual Color GetRenderSnapHandleColour()
        {
            return BoxColour;
        }

        protected virtual Color GetRenderTextColour()
        {
            return BoxColour;
        }

        protected virtual Color GetRenderResizeHoverColour()
        {
            return FillColour;
        }

        protected virtual void RenderBox(Viewport2D viewport, Vector3 start, Vector3 end)
        {
            PrimitiveDrawing.Begin(PrimitiveType.QuadList);
            PrimitiveDrawing.SetColor(GetRenderFillColour());
            PrimitiveDrawing.Vertex3(start.DX, start.DY, start.DZ);
            PrimitiveDrawing.Vertex3(end.DX, start.DY, start.DZ);
            PrimitiveDrawing.Vertex3(end.DX, end.DY, start.DZ);
            PrimitiveDrawing.Vertex3(start.DX, end.DY, start.DZ);
            PrimitiveDrawing.End();

            PrimitiveDrawing.Begin(PrimitiveType.LineList);
            PrimitiveDrawing.SetColor(GetRenderBoxColour());
            PrimitiveDrawing.DottedLine(new Vector3(start.X, start.Y, start.Z), new Vector3(end.X, start.Y, start.Z), 8m / viewport.Zoom);
            PrimitiveDrawing.DottedLine(new Vector3(end.X, start.Y, start.Z), new Vector3(end.X, end.Y, start.Z), 8m / viewport.Zoom);
            PrimitiveDrawing.DottedLine(new Vector3(end.X, end.Y, start.Z), new Vector3(start.X, end.Y, start.Z), 8m / viewport.Zoom);
            PrimitiveDrawing.DottedLine(new Vector3(start.X, end.Y, start.Z), new Vector3(start.X, start.Y, start.Z), 8m / viewport.Zoom);
            PrimitiveDrawing.End();
        }

        protected virtual bool ShouldRenderSnapHandle(Viewport2D viewport)
        {
            return State.Action == BoxAction.Resizing && State.Handle == ResizeHandle.Center;
        }

        protected virtual void RenderSnapHandle(Viewport2D viewport)
        {
            var start = GetResizeOrigin(viewport);
            if (start == null) return;
            const int size = 6;
            var dist = (double)(size / viewport.Zoom);

            var origin = start + viewport.Flatten(State.BoxStart - State.PreTransformBoxStart);
            PrimitiveDrawing.Begin(PrimitiveType.LineList);
            PrimitiveDrawing.SetColor(GetRenderSnapHandleColour());
            PrimitiveDrawing.Vertex3(origin.DX - dist, origin.DY + dist, 0);
            PrimitiveDrawing.Vertex3(origin.DX + dist, origin.DY - dist, 0);
            PrimitiveDrawing.Vertex3(origin.DX + dist, origin.DY + dist, 0);
            PrimitiveDrawing.Vertex3(origin.DX - dist, origin.DY - dist, 0);
            PrimitiveDrawing.End();
        }

        protected virtual bool ShouldRenderResizeBox(Viewport2D viewport)
        {
            return State.ActiveViewport == viewport &&
                   (State.Action == BoxAction.ReadyToResize
                    || State.Action == BoxAction.DownToResize);
        }

        protected virtual double[] GetRenderResizeBox(Vector3 start, Vector3 end)
        {
            var width = Math.Abs(start.DX - end.DX);
            var height = Math.Abs(start.DY - end.DY);
            double x1, y1, x2, y2;
            var handleWidth = width / 10d;
            var handleHeight = height / 10d;
            switch (State.Handle)
            {
                case ResizeHandle.TopLeft:
                    x1 = start.DX;
                    x2 = x1 + handleWidth;
                    y1 = end.DY - handleHeight;
                    y2 = end.DY;
                    break;
                case ResizeHandle.Top:
                    x1 = start.DX;
                    x2 = end.DX;
                    y1 = end.DY - handleHeight;
                    y2 = end.DY;
                    break;
                case ResizeHandle.TopRight:
                    x1 = end.DX - handleWidth;
                    x2 = end.DX;
                    y1 = end.DY - handleHeight;
                    y2 = end.DY;
                    break;
                case ResizeHandle.Left:
                    x1 = start.DX;
                    x2 = x1 + handleWidth;
                    y1 = start.DY;
                    y2 = end.DY;
                    break;
                case ResizeHandle.Center:
                    x1 = start.DX;
                    x2 = end.DX;
                    y1 = start.DY;
                    y2 = end.DY;
                    break;
                case ResizeHandle.Right:
                    x1 = end.DX - handleWidth;
                    x2 = end.DX;
                    y1 = start.DY;
                    y2 = end.DY;
                    break;
                case ResizeHandle.BottomLeft:
                    x1 = start.DX;
                    x2 = x1 + handleWidth;
                    y1 = start.DY;
                    y2 = y1 + handleHeight;
                    break;
                case ResizeHandle.Bottom:
                    x1 = start.DX;
                    x2 = end.DX;
                    y1 = start.DY;
                    y2 = y1 + handleHeight;
                    break;
                case ResizeHandle.BottomRight:
                    x1 = end.DX - handleWidth;
                    x2 = end.DX;
                    y1 = start.DY;
                    y2 = y1 + handleHeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new[] { x1, y1, x2, y2 };
        }

        protected virtual void RenderResizeBox(Viewport2D viewport, Vector3 start, Vector3 end)
        {
            var box = GetRenderResizeBox(start, end);
            PrimitiveDrawing.Begin(PrimitiveType.QuadList);
            PrimitiveDrawing.SetColor(FillColour);
            double x1 = box[0], y1 = box[1], x2 = box[2], y2 = box[3];
            PrimitiveDrawing.Vertex3(x1, y1, 0);
            PrimitiveDrawing.Vertex3(x2, y1, 0);
            PrimitiveDrawing.Vertex3(x2, y2, 0);
            PrimitiveDrawing.Vertex3(x1, y2, 0);
            PrimitiveDrawing.End();
        }

        protected void RenderBoxText(Viewport2D viewport, Vector3 boxStart, Vector3 boxEnd) {
            if (!CBRE.Settings.View.DrawBoxText) return;

            var widthText = (Math.Round(boxEnd.X - boxStart.X, 1)).ToString("#.##");
            var heightText = (Math.Round(boxEnd.Y - boxStart.Y, 1)).ToString("#.##");

            boxStart = viewport.WorldToScreen(boxStart);
            boxEnd = viewport.WorldToScreen(boxEnd);

            #warning TODO: reimplement
        }

        protected virtual void Render2D(Viewport2D viewport)
        {
            if (State.Action == BoxAction.ReadyToDraw || State.Action == BoxAction.DownToDraw) return;
            var start = viewport.Flatten(State.BoxStart);
            var end = viewport.Flatten(State.BoxEnd);
            if (ShouldDrawBox(viewport))
            {
                RenderBox(viewport, start, end);
            }
            if (ShouldRenderSnapHandle(viewport))
            {
                RenderSnapHandle(viewport);
            }
            if (ShouldRenderResizeBox(viewport))
            {
                RenderResizeBox(viewport, start, end);
            }
            if (ShouldDrawBoxText(viewport))
            {
                RenderBoxText(viewport, start, end);
            }
        }

        protected virtual bool ShouldDraw3DBox()
        {
            return State.Action != BoxAction.ReadyToDraw;
        }

        protected virtual void Render3DBox(Viewport3D viewport, Vector3 start, Vector3 end)
        {
            var box = new Box(start, end);

            PrimitiveDrawing.Begin(PrimitiveType.LineList);
            PrimitiveDrawing.SetColor(GetRenderBoxColour());
            foreach (var line in box.GetBoxLines())
            {
                PrimitiveDrawing.Vertex3(line.Start);
                PrimitiveDrawing.Vertex3(line.End);
            }
            PrimitiveDrawing.End();
        }

        protected virtual void Render3D(Viewport3D viewport)
        {
            if (State.Action == BoxAction.ReadyToDraw || State.Action == BoxAction.DownToDraw) return;
            if (ShouldDraw3DBox())
            {
                Render3DBox(viewport, State.BoxStart, State.BoxEnd);
            }
        }
        #endregion

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame)
        {

        }

        public override void MouseEnter(ViewportBase viewport, ViewportEvent e)
        {
            if (State.ActiveViewport != null) State.ActiveViewport.Cursor = MouseCursor.Arrow;
        }

        public override void MouseLeave(ViewportBase viewport, ViewportEvent e)
        {
            if (State.ActiveViewport != null) State.ActiveViewport.Cursor = MouseCursor.Arrow;
        }

        protected bool GetSelectionBox(out Box boundingbox)
        {
            // If one of the dimensions has a depth value of 0, extend it out into infinite space
            // If two or more dimensions have depth 0, do nothing.

            var sameX = State.BoxStart.X == State.BoxEnd.X;
            var sameY = State.BoxStart.Y == State.BoxEnd.Y;
            var sameZ = State.BoxStart.Z == State.BoxEnd.Z;
            var start = State.BoxStart.Clone();
            var end = State.BoxEnd.Clone();
            var invalid = false;

            if (sameX)
            {
                if (sameY || sameZ) invalid = true;
                start.X = Decimal.MinValue;
                end.X = Decimal.MaxValue;
            }

            if (sameY)
            {
                if (sameZ) invalid = true;
                start.Y = Decimal.MinValue;
                end.Y = Decimal.MaxValue;
            }

            if (sameZ)
            {
                start.Z = Decimal.MinValue;
                end.Z = Decimal.MaxValue;
            }

            boundingbox = new Box(start, end);
            return !invalid;
        }
    }
}
#pragma warning restore 0612

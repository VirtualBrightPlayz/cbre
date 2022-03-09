using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.Graphics;
using CBRE.Settings;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Camera = CBRE.DataStructures.MapObjects.Camera;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Tools
{
    public class CameraTool : BaseTool
    {
        private enum State
        {
            None,
            MovingPosition,
            MovingLook,
            Moving3d
        }

        [Flags]
        private enum MoveFlags {
            None = 0x0,
            Left = 0x1,
            Right = 0x2,
            Up = 0x4,
            Down = 0x8
        }

        private BaseTool? capturedTool = null;
        private State _state;
        private Camera _stateCamera;

        public override void ToolSelected(bool preventHistory)
        {
            _state = State.None;
            Mediator.Subscribe(HotkeysMediator.CameraNext, this);
            Mediator.Subscribe(HotkeysMediator.CameraPrevious, this);
        }

        private void CameraNext()
        {
            if (_state != State.None || Document.Map.Cameras.Count < 2) return;
            var idx = Document.Map.Cameras.IndexOf(Document.Map.ActiveCamera);
            idx = (idx + 1) % Document.Map.Cameras.Count;
            Document.Map.ActiveCamera = Document.Map.Cameras[idx];
            SetViewportCamera(Document.Map.ActiveCamera.EyePosition, Document.Map.ActiveCamera.LookPosition);
        }

        private void CameraPrevious()
        {
            if (_state != State.None || Document.Map.Cameras.Count < 2) return;
            var idx = Document.Map.Cameras.IndexOf(Document.Map.ActiveCamera);
            idx = (idx + Document.Map.Cameras.Count - 1) % Document.Map.Cameras.Count;
            Document.Map.ActiveCamera = Document.Map.Cameras[idx];
            SetViewportCamera(Document.Map.ActiveCamera.EyePosition, Document.Map.ActiveCamera.LookPosition);
        }

        private void CameraDelete()
        {
            if (_state != State.None || Document.Map.Cameras.Count < 2) return;
            var del = Document.Map.ActiveCamera;
            CameraPrevious();
            if (del != Document.Map.ActiveCamera) Document.Map.Cameras.Remove(del);
        }

        public override string GetIcon()
        {
            return "Tool_Camera";
        }

        public override string GetName()
        {
            return "Camera Tool";
        }

        public override HotkeyTool? GetHotkeyToolType()
        {
            return HotkeyTool.Camera;
        }

        public override string GetContextualHelp()
        {
            return "*Click* the camera origin or direction arrow to move the camera.\n" +
                   "Hold *shift* and *click* to create multiple cameras.\n" +
                   "Press *Tab* to cycle between cameras";
        }

        private Tuple<Vector3, Vector3> GetViewportCamera()
        {
            var cam = ViewportManager.Viewports.OfType<Viewport3D>().Select(x => x.Camera).FirstOrDefault();
            if (cam == null) return null;

            var pos = cam.EyePosition;
            var look = cam.LookPosition;

            var dir = (look - pos).Normalise() * 20;
            return Tuple.Create(pos, pos + dir);
        }

        private void SetViewportCamera(Vector3 position, Vector3 look, Camera cam = null)
        {
            cam ??= ViewportManager.Viewports.OfType<Viewport3D>().Select(x => x.Camera).FirstOrDefault();
            if (cam == null) return;

            look = (look - position).Normalise() + position;
            cam.EyePosition = position;
            cam.LookPosition = look;

            ViewportManager.MarkForRerender();
        }

        private State GetStateAtPoint(int x, int y, Viewport2D viewport, out Camera activeCamera)
        {
            var d = 5m / viewport.Zoom;

            foreach (var cam in GetCameras())
            {
                var p = viewport.ScreenToWorld(x, y);
                var pos = viewport.Flatten(cam.EyePosition);
                var look = viewport.Flatten(cam.LookPosition);
                activeCamera = cam;
                if (p.X >= pos.X - d && p.X <= pos.X + d && p.Y >= pos.Y - d && p.Y <= pos.Y + d) return State.MovingPosition;
                if (p.X >= look.X - d && p.X <= look.X + d && p.Y >= look.Y - d && p.Y <= look.Y + d) return State.MovingLook;
            }

            activeCamera = null;
            return State.None;
        }

        private IEnumerable<Camera> GetCameras()
        {
            var c = GetViewportCamera();
            if (!Document.Map.Cameras.Any())
            {
                Document.Map.Cameras.Add(new Camera { EyePosition = c.Item1, LookPosition = c.Item2 });
            }
            if (Document.Map.ActiveCamera == null || !Document.Map.Cameras.Contains(Document.Map.ActiveCamera))
            {
                Document.Map.ActiveCamera = Document.Map.Cameras.First();
            }
            var len = Document.Map.ActiveCamera.Length;
            Document.Map.ActiveCamera.EyePosition = c.Item1;
            Document.Map.ActiveCamera.LookPosition = c.Item1 + (c.Item2 - c.Item1).Normalise() * len;
            foreach (var camera in Document.Map.Cameras)
            {
                var dir = camera.LookPosition - camera.EyePosition;
                camera.LookPosition = camera.EyePosition + dir.Normalise() * Math.Max(Document.Map.GridSpacing * 1.5m, dir.VectorMagnitude());
                yield return camera;
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

        public override void MouseDown(ViewportBase viewport, ViewportEvent e)
        {
            if (viewport is Viewport2D vp) {
                _state = GetStateAtPoint(e.X, vp.Height - e.Y, vp, out _stateCamera);
                if (_state == State.None && ViewportManager.Shift) {
                    var p = SnapIfNeeded(vp.Expand(vp.ScreenToWorld(e.X, vp.Height - e.Y)));
                    _stateCamera = new Camera { EyePosition = p, LookPosition = p + Vector3.UnitX * 1.5m * Document.Map.GridSpacing };
                    Document.Map.Cameras.Add(_stateCamera);
                    _state = State.MovingLook;
                }
                if (_stateCamera != null) {
                    SetViewportCamera(_stateCamera.EyePosition, _stateCamera.LookPosition);
                    Document.Map.ActiveCamera = _stateCamera;
                }
            } else if (viewport is Viewport3D vp3d) {
                _state = State.Moving3d;
                ViewportManager.SetCursorPos(vp3d, vp3d.Width / 2, vp3d.Height / 2);
            }
        }

        public override void MouseClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
        }

        public override void MouseDoubleClick(ViewportBase viewport, ViewportEvent e)
        {
            // Not used
        }

        public override void MouseUp(ViewportBase viewport, ViewportEvent e)
        {
            _state = State.None;
        }

        public override void MouseWheel(ViewportBase viewport, ViewportEvent e)
        {
            //
        }

        public override void MouseMove(ViewportBase viewport, ViewportEvent e) {
            switch (viewport)
            {
                case Viewport2D vp:
                {
                    var p = SnapIfNeeded(vp.Expand(vp.ScreenToWorld(e.X, vp.Height - e.Y)));
                    var cursor = MouseCursor.Arrow;

                    switch (_state) {
                        case State.None:
                            var st = GetStateAtPoint(e.X, vp.Height - e.Y, vp, out _stateCamera);
                            if (st != State.None) cursor = MouseCursor.SizeAll;
                            break;
                        case State.MovingPosition:
                            if (_stateCamera == null) break;
                            var newEye = vp.GetUnusedCoordinate(_stateCamera.EyePosition) + p;
                            if (ViewportManager.Ctrl) _stateCamera.LookPosition += (newEye - _stateCamera.EyePosition);
                            _stateCamera.EyePosition = newEye;
                            if (Document.Map.ActiveCamera == _stateCamera) { SetViewportCamera(_stateCamera.EyePosition, _stateCamera.LookPosition); }
                            break;
                        case State.MovingLook:
                            if (_stateCamera == null) break;
                            var newLook = vp.GetUnusedCoordinate(_stateCamera.LookPosition) + p;
                            if (ViewportManager.Ctrl) _stateCamera.EyePosition += (newLook - _stateCamera.LookPosition);
                            _stateCamera.LookPosition = newLook;
                            if (Document.Map.ActiveCamera == _stateCamera) { SetViewportCamera(_stateCamera.EyePosition, _stateCamera.LookPosition); }
                            break;
                    }
                    vp.Cursor = cursor;
                    break;
                }
                case Viewport3D vp3d when _state == State.Moving3d:
                {
                    //if (!FreeLook) return;

                    var camera = GetCameras().FirstOrDefault();

                    var left = e.Button.HasFlag(MouseButtons.Left);
                    var right = e.Button.HasFlag(MouseButtons.Right);
                    var updown = !left && right;
                    var forwardback = left && right;

                    int dx = -e.DeltaX; int dy = e.DeltaY;

                    if (CBRE.Settings.View.InvertX) dx = -dx;
                    if (CBRE.Settings.View.InvertY) dy = -dy;

                    if (updown) {
                        camera.Strafe(-dx);
                        camera.Ascend(-dy);
                    } else if (forwardback) {
                        camera.Strafe(-dx);
                        camera.Advance(-dy);
                    } else { // left mouse or z-toggle
                        var fovdiv = (vp3d.Width / 60m) / 2.5m;
                        camera.Pan(dx / fovdiv);
                        camera.Tilt(dy / fovdiv);
                    }
                
                    ViewportManager.SetCursorPos(vp3d, vp3d.Width / 2, vp3d.Height / 2);
                    SetViewportCamera(camera.EyePosition, camera.LookPosition, vp3d.Camera);
                    break;
                }
            }
        }

        public override void KeyPressBackground(ViewportBase viewport, ViewportEvent e) {
            if (viewport is Viewport3D && e.KeyCode == Keys.Z) {
                capturedTool = GameMain.Instance.SelectedTool;
                GameMain.Instance.SelectedTool = this;
                _state = State.Moving3d;
                ViewportManager.SetCursorPos(viewport, viewport.Width / 2, viewport.Height / 2);
            }
        }
        
        public override void KeyUpBackground(ViewportBase viewport, ViewportEvent e) {
            if (e.KeyCode == Keys.Z && capturedTool != null) {
                GameMain.Instance.SelectedTool = capturedTool;
                capturedTool = null;
                _state = State.None;
            }
        }

        public override void KeyPress(ViewportBase viewport, ViewportEvent e)
        {
            //
        }

        public override void KeyDown(ViewportBase viewport, ViewportEvent e) {
            //
        }
        public override void KeyUp(ViewportBase viewport, ViewportEvent e) {
            //
        }

        public override void KeyDown(ViewportEvent e) {
            //
        }

        public override void KeyUp(ViewportEvent e) {
            //
        }

        public override void Update() {
            //
        }

        public override void UpdateFrame(ViewportBase viewport, FrameInfo frame) {
            //
        }

        public override void Render(ViewportBase viewport)
        {
            var vp = viewport as Viewport2D;
            if (vp == null) return;

            var cams = GetCameras().ToList();
            if (!cams.Any()) return;

            var z = (double)vp.Zoom;

            /*GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);*/

            // Draw lines between points and point outlines
            PrimitiveDrawing.Begin(PrimitiveType.LineList);

            foreach (var camera in cams)
            {
                var p1 = vp.Flatten(camera.EyePosition);
                var p2 = vp.Flatten(camera.LookPosition);

                PrimitiveDrawing.SetColor(camera == Document.Map.ActiveCamera ? Color.Red : Color.Cyan);
                PrimitiveDrawing.Vertex2(p1.DX, p1.DY);
                PrimitiveDrawing.Vertex2(p2.DX, p2.DY);
                PrimitiveDrawing.Vertex2(p2.DX, p2.DY);
                PrimitiveDrawing.Vertex2(p1.DX, p1.DY);
            }

            PrimitiveDrawing.End();

            /*GL.Enable(EnableCap.PolygonSmooth);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);*/

            foreach (var camera in cams)
            {
                var p1 = vp.Flatten(camera.EyePosition);

                // Position circle
                PrimitiveDrawing.Begin(PrimitiveType.TriangleFan);
                PrimitiveDrawing.SetColor(camera == Document.Map.ActiveCamera ? Color.DarkOrange : Color.LawnGreen);
                PrimitiveDrawing.Circle(new Vector3(p1.X, p1.Y, (decimal)z), (double)(4m / vp.Zoom));
                PrimitiveDrawing.End();
            }
            foreach (var camera in cams)
            {
                var p1 = vp.Flatten(camera.EyePosition);
                var p2 = vp.Flatten(camera.LookPosition);

                var multiplier = 4 / vp.Zoom;
                var dir = (p2 - p1).Normalise();
                var cp = new Vector3(-dir.Y, dir.X, 0).Normalise();

                // Direction Triangle
                PrimitiveDrawing.Begin(PrimitiveType.TriangleList);
                PrimitiveDrawing.SetColor(camera == Document.Map.ActiveCamera ? Color.Red : Color.Cyan);
                Coord(p2 - (dir - cp) * multiplier);
                Coord(p2 - (dir + cp) * multiplier);
                Coord(p2 + dir * 1.5m * multiplier);
                PrimitiveDrawing.End();
            }

            //GL.Disable(EnableCap.PolygonSmooth);

            PrimitiveDrawing.Begin(PrimitiveType.LineList);

            foreach (var camera in cams)
            {
                var p1 = vp.Flatten(camera.EyePosition);
                var p2 = vp.Flatten(camera.LookPosition);

                var multiplier = 4 / vp.Zoom;
                var dir = (p2 - p1).Normalise();
                var cp = new Vector3(-dir.Y, dir.X, 0).Normalise();

                PrimitiveDrawing.SetColor(Color.Black);
                PrimitiveDrawing.Circle(new Vector3(p1.X, p1.Y, (decimal)z), (double)(4m / vp.Zoom));
                Coord(p2 + dir * 1.5m * multiplier);
                Coord(p2 - (dir + cp) * multiplier);
                Coord(p2 - (dir + cp) * multiplier);
                Coord(p2 - (dir - cp) * multiplier);
                Coord(p2 - (dir - cp) * multiplier);
                Coord(p2 + dir * 1.5m * multiplier);
            }

            PrimitiveDrawing.End();

            //GL.Disable(EnableCap.LineSmooth);
        }

        protected static void Coord(Vector3 c)
        {
            PrimitiveDrawing.Vertex3(new Microsoft.Xna.Framework.Vector3((float)c.DX, (float)c.DY, (float)c.DZ));
        }

        public override HotkeyInterceptResult InterceptHotkey(HotkeysMediator hotkeyMessage, object parameters)
        {
            if (hotkeyMessage == HotkeysMediator.OperationsDelete)
            {
                CameraDelete();
                return HotkeyInterceptResult.Abort;
            }
            return HotkeyInterceptResult.Continue;
        }
    }
}

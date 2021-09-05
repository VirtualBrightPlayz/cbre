using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Editor.Rendering {
    public enum MouseButtons {
        None = 0x0,
        Left = 0x1,
        Right = 0x2,
        Middle = 0x4,
        Mouse4 = 0x8,
        Mouse5 = 0x10
    }

    public class ViewportEvent : EventArgs {
        public bool Handled { get; set; }

        // Key
        //public Keys Modifiers { get; set; }
        public bool Control { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        
        public Keys KeyCode { get; set; }
        public int KeyValue { get; set; }
        public char KeyChar { get; set; }

        // Mouse
        public MouseButtons Button { get; set; }
        public int Clicks { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Delta { get; set; }

        public Vector3 Location => new Vector3(X, Y, 0);

        // Mouse movement
        public int LastX { get; set; }
        public int LastY { get; set; }

        public int DeltaX => X - LastX;
        public int DeltaY => Y - LastY;

        // Click and drag
        public bool Dragging { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }

        // 2D Camera
        public Vector3 CameraPosition { get; set; }
        public decimal CameraZoom { get; set; }
    }

    static class ViewportManager {
        public static ViewportBase[] Viewports { get; private set; } = new ViewportBase[4];
        static Vector2 splitPoint = new Vector2(0.45f, 0.55f);
        static VertexPositionColor[] backgroundVertices = new VertexPositionColor[12];
        static short[] backgroundIndices = new short[18];
        static BasicEffect basicEffect = null;

        static bool prevMouse1Down;
        static bool prevMouse2Down;
        static bool prevMouse3Down;
        static int prevScrollWheelValue;
        static Keys[] prevKeysDown;

        static bool draggingCenterX; static bool draggingCenterY;
        static int draggingViewport;

        public static RenderTarget2D renderTarget { get; private set; }
        public static IntPtr renderTargetPtr { get; private set; }
        private static VertexPositionTexture[] renderTargetGeom;
        static BasicEffect renderTargetEffect = null;

        public static bool Ctrl { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Alt { get; private set; }


        public static Rectangle vpRect { get; set; } = new Rectangle(0, 0, 640, 480);

        public static void Init() {
            prevMouse1Down = false;
            prevMouse3Down = false;
            prevKeysDown = new Keys[0];
            draggingCenterX = false;
            draggingCenterY = false;
            draggingViewport = -1;

            Viewports[0] = new Viewport3D(Viewport3D.ViewType.Textured);
            Viewports[1] = new Viewport2D(Viewport2D.ViewDirection.Top);
            Viewports[2] = new Viewport2D(Viewport2D.ViewDirection.Side);
            Viewports[3] = new Viewport2D(Viewport2D.ViewDirection.Front);

            basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            basicEffect.World = Matrix.Identity;
            basicEffect.View = Matrix.Identity;
            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;

            renderTargetEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            renderTargetEffect.World = Matrix.Identity;
            renderTargetEffect.View = Matrix.Identity;
            renderTargetEffect.TextureEnabled = true;
            renderTargetEffect.VertexColorEnabled = false;

            backgroundVertices[0] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };
            backgroundVertices[1] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };
            backgroundVertices[2] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };
            backgroundVertices[3] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Black };

            backgroundVertices[4] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[5] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[6] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };
            backgroundVertices[7] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };

            backgroundVertices[8] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[9] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.White };
            backgroundVertices[10] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };
            backgroundVertices[11] = new VertexPositionColor() { Position = new Vector3(0, 0, 0), Color = Color.Gray };

            backgroundIndices[0] = 0;
            backgroundIndices[1] = 1;
            backgroundIndices[2] = 2;
            backgroundIndices[3] = 1;
            backgroundIndices[4] = 2;
            backgroundIndices[5] = 3;

            backgroundIndices[6] = 4;
            backgroundIndices[7] = 5;
            backgroundIndices[8] = 6;
            backgroundIndices[9] = 5;
            backgroundIndices[10] = 6;
            backgroundIndices[11] = 7;

            backgroundIndices[12] = 8;
            backgroundIndices[13] = 9;
            backgroundIndices[14] = 10;
            backgroundIndices[15] = 9;
            backgroundIndices[16] = 10;
            backgroundIndices[17] = 11;

            RebuildRenderTarget();

            AsyncTexture.LoadCallback = TextureLoadCallback;
        }

        private static int knownWindowWidth = 0;
        private static int knownWindowHeight = 0;

        public static bool TopMenuOpen = false;

        private static void RebuildRenderTarget() {
            renderTargetGeom = new VertexPositionTexture[] {
                new VertexPositionTexture(new Vector3(vpRect.Location.X, vpRect.Location.Y, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(vpRect.Right, vpRect.Location.Y, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(vpRect.Location.X, vpRect.Bottom, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(vpRect.Right, vpRect.Bottom, 0), new Vector2(1, 1)),
            };
            knownWindowWidth = vpRect.Right;
            knownWindowHeight = vpRect.Bottom;
            renderTargetEffect.Projection = Matrix.CreateOrthographicOffCenter(0.5f, GlobalGraphics.Window.ClientBounds.Width + 0.5f, GlobalGraphics.Window.ClientBounds.Height + 0.5f, 0.5f, -1f, 1f);
            if (renderTargetPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(renderTargetPtr);
            }
            renderTarget?.Dispose();
            renderTarget = new RenderTarget2D(GlobalGraphics.GraphicsDevice, vpRect.Right - vpRect.Location.X, vpRect.Bottom - vpRect.Location.Y, false, SurfaceFormat.Color, DepthFormat.Depth24);
            renderTargetPtr = GlobalGraphics.ImGuiRenderer.BindTexture(renderTarget);

            int splitX = (int)((vpRect.Right - vpRect.Location.X) * splitPoint.X) + vpRect.Location.X;
            int splitY = (int)((vpRect.Bottom - vpRect.Location.Y) * splitPoint.Y) + vpRect.Location.Y;
            for (int i = 0; i < Viewports.Length; i++) {
                if (Viewports[i] == null) { continue; }
                bool left = i % 2 == 0;
                bool top = i < 2;

                Viewports[i].X = left ? vpRect.Location.X : splitX + 3;
                Viewports[i].Y = top ? vpRect.Location.Y : splitY + 3;
                Viewports[i].Width = left ? splitX - vpRect.Location.X - 4 : renderTarget.Width - (splitX - vpRect.Location.X + 3);
                Viewports[i].Height = top ? splitY - vpRect.Location.Y - 4 : renderTarget.Height - (splitY - vpRect.Location.Y + 3);
            }

            Render();
        }

        private static bool shouldRerender = false;

        public static void MarkForRerender() {
            shouldRerender = true;
        }

        public static void TextureLoadCallback(string texName) {
            Documents.DocumentManager.Documents.ForEach(d => d.ObjectRenderer.MarkDirty(texName));
            MarkForRerender();
        }

        public static void SetCursorPos(ViewportBase vp, int posX, int posY) {
            Mouse.SetPosition(posX + vp.X, posY + vp.Y);
        }

        public static void Update() {
            GameMain.Instance.SelectedTool?.Update();
            var doc = DocumentManager.CurrentDocument;
            if (doc != null && doc.LightmapTextureOutdated) {
                doc.LightmapTextureOutdated = false;
                shouldRerender = true;
            }
            var mouseState = Mouse.GetState();
            var keyboardState = Keyboard.GetState();
            var keysDown = keyboardState.GetPressedKeys();
            bool mouse1Down = mouseState.LeftButton == ButtonState.Pressed;
            bool mouse1Hit = mouse1Down && !prevMouse1Down;
            bool mouse2Down = mouseState.RightButton == ButtonState.Pressed;
            bool mouse2Hit = mouse2Down && !prevMouse2Down;
            bool mouse3Down = mouseState.MiddleButton == ButtonState.Pressed;
            bool mouse3Hit = mouse3Down && !prevMouse3Down;
            int scrollWheelValue = mouseState.ScrollWheelValue;
            Ctrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            Shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            Alt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

            foreach (var key in keysDown.Where(k => !prevKeysDown.Contains(k))) {
                GameMain.Instance.SelectedTool?.KeyDown(new ViewportEvent() {
                    Handled = false,
                    KeyCode = key
                });
            }

            foreach (var key in prevKeysDown.Where(k => !keysDown.Contains(k))) {
                GameMain.Instance.SelectedTool?.KeyUp(new ViewportEvent() {
                    Handled = false,
                    KeyCode = key
                });
            }

            for (int i = 0; i < GameMain.Instance.Popups.Count; i++) {
                if (GameMain.Instance.Popups[i] is ViewportWindow viewportWindow) {

                    viewportWindow.viewport.X = viewportWindow.view.Location.X;
                    viewportWindow.viewport.Y = viewportWindow.view.Location.Y;
                    viewportWindow.viewport.Width = viewportWindow.view.Size.X;
                    viewportWindow.viewport.Height = viewportWindow.view.Size.Y;

                    if (viewportWindow.IsOverAndOpen(mouseState)) {

                        if (!mouse1Down && !mouse3Down) {
                            GameMain.Instance.SelectedTool?.MouseUp(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = mouseState.X - viewportWindow.viewport.X,
                                Y = mouseState.Y - viewportWindow.viewport.Y,
                                LastX = viewportWindow.viewport.PrevMouseX,
                                LastY = viewportWindow.viewport.PrevMouseY,
                            });
                            MarkForRerender();
                            draggingCenterX = false;
                            draggingCenterY = false;
                            draggingViewport = -1;
                        }

                        GameMain.Instance.SelectedTool?.UpdateFrame(viewportWindow.viewport, new FrameInfo(0)); //TODO: fix FrameInfo

                        foreach (var key in keysDown.Where(k => !prevKeysDown.Contains(k))) {
                            GameMain.Instance.SelectedTool?.KeyDown(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                KeyCode = key
                            });
                        }

                        foreach (var key in prevKeysDown.Where(k => !keysDown.Contains(k))) {
                            GameMain.Instance.SelectedTool?.KeyUp(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                KeyCode = key
                            });
                        }

                        if (viewportWindow.viewport is Viewport3D vp3d) {
                            bool shiftDown = mouse2Down;
                            bool mustRerender = false;
                            // WASD
                            if (keyboardState.IsKeyDown(Keys.A)) {
                                vp3d.Camera.Strafe(-5m - (shiftDown ? 5m : 0m));
                                mustRerender = true;
                            }
                            if (keyboardState.IsKeyDown(Keys.D)) {
                                vp3d.Camera.Strafe(5m + (shiftDown ? 5m : 0m));
                                mustRerender = true;
                            }
                            if (keyboardState.IsKeyDown(Keys.W)) {
                                vp3d.Camera.Advance(5m + (shiftDown ? 5m : 0m));
                                mustRerender = true;
                            }
                            if (keyboardState.IsKeyDown(Keys.S)) {
                                vp3d.Camera.Advance(-5m - (shiftDown ? 5m : 0m));
                                mustRerender = true;
                            }
                            // look around
                            var fovdiv = (vp3d.Width / 60m) / 2.5m;
                            if (keyboardState.IsKeyDown(Keys.Left)) {
                                vp3d.Camera.Pan(5m / fovdiv);
                                mustRerender = true;
                            }
                            if (keyboardState.IsKeyDown(Keys.Right)) {
                                vp3d.Camera.Pan(-5m / fovdiv);
                                mustRerender = true;
                            }
                            if (keyboardState.IsKeyDown(Keys.Up)) {
                                vp3d.Camera.Tilt(-5m / fovdiv);
                                mustRerender = true;
                            }
                            if (keyboardState.IsKeyDown(Keys.Down)) {
                                vp3d.Camera.Tilt(5m / fovdiv);
                                mustRerender = true;
                            }
                            if (mustRerender) {
                                var map = Documents.DocumentManager.CurrentDocument.Map;
                                if (map.ActiveCamera == null) { map.ActiveCamera = map.Cameras.FirstOrDefault(); }
                                if (map.ActiveCamera != null) {
                                    map.ActiveCamera.EyePosition = vp3d.Camera.EyePosition;
                                    map.ActiveCamera.LookPosition = vp3d.Camera.LookPosition;
                                }
                                MarkForRerender();
                            }
                        }

                        int currMouseX = mouseState.X - viewportWindow.viewport.X;
                        int currMouseY = mouseState.Y - viewportWindow.viewport.Y;
                        if (viewportWindow.viewport.PrevMouseX != currMouseX || viewportWindow.viewport.PrevMouseY != currMouseY) {
                            GameMain.Instance.SelectedTool?.MouseMove(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewportWindow.viewport.PrevMouseX,
                                LastY = viewportWindow.viewport.PrevMouseY,
                            });
                            MarkForRerender();
                        }

                        if (mouse1Hit) {
                            GameMain.Instance.SelectedTool?.MouseClick(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewportWindow.viewport.PrevMouseX,
                                LastY = viewportWindow.viewport.PrevMouseY,
                                Clicks = 1
                            });

                            GameMain.Instance.SelectedTool?.MouseDown(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewportWindow.viewport.PrevMouseX,
                                LastY = viewportWindow.viewport.PrevMouseY,
                            });
                        }
                        if (mouse2Hit) {
                            GameMain.Instance.SelectedTool?.MouseClick(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Right,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewportWindow.viewport.PrevMouseX,
                                LastY = viewportWindow.viewport.PrevMouseY,
                                Clicks = 1
                            });

                            GameMain.Instance.SelectedTool?.MouseDown(viewportWindow.viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Right,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewportWindow.viewport.PrevMouseX,
                                LastY = viewportWindow.viewport.PrevMouseY,
                            });
                        }
                        if (mouse3Down) {
                            if (viewportWindow.viewport is Viewport2D vp) {
                                MarkForRerender();
                                vp.Position -= new DataStructures.Geometric.Vector3((decimal)(currMouseX - vp.PrevMouseX) / vp.Zoom, -(decimal)(currMouseY - vp.PrevMouseY) / vp.Zoom, 0m);
                            }
                        }
                    }
                }
            }

            mouseState = Mouse.GetState();

            for (int i = 0; i < GameMain.Instance.Popups.Count; i++) {
                if (GameMain.Instance.Popups[i] is ViewportWindow viewportWindow) {
                    bool mouseOver = false;
                    if (viewportWindow.IsOverAndOpen(mouseState)) {
                        mouseOver = true;
                    }
                    viewportWindow.viewport.PrevMouseOver = mouseOver;
                    viewportWindow.viewport.PrevMouseX = mouseState.X - viewportWindow.viewport.X;
                    viewportWindow.viewport.PrevMouseY = mouseState.Y - viewportWindow.viewport.Y;

                    if (mouseOver && scrollWheelValue != prevScrollWheelValue) {
                        if (viewportWindow.viewport is Viewport2D vp) {
                            var pos0 = vp.ScreenToWorld(new System.Drawing.Point(mouseState.X - viewportWindow.viewport.X, mouseState.Y - viewportWindow.viewport.Y));
                            decimal scrollWheelDiff = (scrollWheelValue - prevScrollWheelValue) * 0.001m;
                            if (scrollWheelDiff > 0m) {
                                vp.Zoom *= 1.0m + scrollWheelDiff;
                            } else {
                                vp.Zoom /= 1.0m - scrollWheelDiff;
                            }
                            var pos1 = vp.ScreenToWorld(new System.Drawing.Point(mouseState.X - viewportWindow.viewport.X, mouseState.Y - viewportWindow.viewport.Y));
                            vp.Position -= new DataStructures.Geometric.Vector3(pos1.X - pos0.X, pos0.Y - pos1.Y, 0m);
                            MarkForRerender();
                        }
                    }
                }
            }

            prevMouse1Down = mouse1Down;
            prevMouse2Down = mouse2Down;
            prevMouse3Down = mouse3Down;
            prevKeysDown = keysDown;
            prevScrollWheelValue = scrollWheelValue;
        }

        public static void RenderIfNecessary() {
            if (!shouldRerender) { return; }
            Render();
        }

        public static void Render() {
            shouldRerender = false;
            for (int i = 0; i < GameMain.Instance.Popups.Count; i++) {
                if (GameMain.Instance.Popups[i] is ViewportWindow viewportWindow) {
                    viewportWindow.ResetRenderTarget();
                }
            }
        }

        public static void Render(ViewportBase viewport, Viewport view, RenderTarget2D renderTarget)
        {
            void resetBasicEffect(ViewportBase viewport) {
                basicEffect.Projection = viewport.GetViewportMatrix();
                basicEffect.View = viewport.GetCameraMatrix();
                basicEffect.World = Microsoft.Xna.Framework.Matrix.Identity;
                basicEffect.CurrentTechnique.Passes[0].Apply();
            };

            if (viewport == null) { return; }

            GlobalGraphics.GraphicsDevice.ScissorRectangle = view.Bounds;
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;
            GlobalGraphics.GraphicsDevice.Viewport = view;

            GlobalGraphics.GraphicsDevice.SetRenderTarget(renderTarget);
            GlobalGraphics.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            resetBasicEffect(viewport);

            viewport.DrawGrid();
            viewport.Render();

            resetBasicEffect(viewport);

            GlobalGraphics.GraphicsDevice.DepthStencilState = viewport is Viewport3D ? DepthStencilState.Default : DepthStencilState.None;
            GameMain.Instance.SelectedTool?.Render(viewport);

            GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
            GlobalGraphics.GraphicsDevice.Viewport = prevViewport;
        }

        public static void DrawRenderTarget() {
            if (ImGuiNET.ImGui.BeginChildFrame(3, new System.Numerics.Vector2(vpRect.Size.X, vpRect.Size.Y))) {
                ImGuiNET.ImGui.Image(renderTargetPtr, new System.Numerics.Vector2(vpRect.Size.X, vpRect.Size.Y));
            }
            ImGuiNET.ImGui.EndChildFrame();
        }
    }
}

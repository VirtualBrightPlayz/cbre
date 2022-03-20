using System;
using System.Linq;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CBRE.Editor.Popup {
    public class ViewportWindow : DockableWindow {
        public ViewportBase[] Viewports => ViewportManager.Viewports;

        public Rectangle WindowRectangle { get; private set; }
        public Vector2 ViewportCenter { get; private set; } = Vector2.One * 0.5f;

        [Flags]
        private enum DraggingMode {
            None = 0x0,
            Horizontal = 0x1,
            Vertical = 0x2
        }
        private DraggingMode draggingCenter = DraggingMode.None;

        private const int viewportGap = 2;
        
        public Viewport GetXnaViewport(int viewportIndex) {
            int centerX = (int)(ViewportCenter.X * WindowRectangle.Width);
            int centerY = (int)(ViewportCenter.Y * WindowRectangle.Height);

            (int left, int right) = viewportIndex % 2 == 0
                ? (0, centerX - viewportGap)
                : (centerX + viewportGap, WindowRectangle.Width);
            (int top, int bottom) = viewportIndex / 2 == 0
                ? (0, centerY - viewportGap)
                : (centerY + viewportGap, WindowRectangle.Height);
            return new Viewport(left, top, right - left, bottom - top);
        }

        public Rectangle GetXnaRectangle(int viewportIndex) {
            var xnaViewport = GetXnaViewport(viewportIndex);
            return new Rectangle(xnaViewport.X + WindowRectangle.X, xnaViewport.Y + WindowRectangle.Y, xnaViewport.Width, xnaViewport.Height);
        }
        
        public RenderTarget2D RenderTarget { get; private set; }
        public IntPtr RenderTargetImGuiPtr { get; private set; }
        public BasicEffect BasicEffect { get; private set; }
        public string Name { get; private set; }
        private bool selected = false;
        
        private readonly static BasicEffect basicEffect;

        static ViewportWindow() {
            basicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            basicEffect.World = Matrix.Identity;
            basicEffect.View = Matrix.Identity;
            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;
        }

        public ViewportWindow() : base("Viewports", ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar) {
            BasicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            Name = "Viewports";
        }

        public void ResetRenderTarget() {
            if (WindowRectangle.Width <= 0 && WindowRectangle.Height <= 0) { return; }
            if (RenderTargetImGuiPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(RenderTargetImGuiPtr);
                RenderTargetImGuiPtr = IntPtr.Zero;
            }
            RenderTarget?.Dispose();
            RenderTarget = new RenderTarget2D(GlobalGraphics.GraphicsDevice, Math.Max(WindowRectangle.Width, 4), Math.Max(WindowRectangle.Height, 4), false, SurfaceFormat.Color, DepthFormat.Depth24);

            GlobalGraphics.GraphicsDevice.SetRenderTarget(RenderTarget);
            GlobalGraphics.GraphicsDevice.Clear(Color.Black);
            for (int i = 0; i < Viewports.Length; i++) {
                Render(i);
            }

            basicEffect.Projection =
                Matrix.CreateTranslation(-RenderTarget.Width / 2, -RenderTarget.Height / 2, 0.0f)
                * Matrix.CreateOrthographic(RenderTarget.Width, RenderTarget.Height, -1.0f, 1.0f);
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;
            basicEffect.CurrentTechnique.Passes[0].Apply();

            GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
            RenderTargetImGuiPtr = GlobalGraphics.ImGuiRenderer.BindTexture(RenderTarget);
        }
        
        private void Render(int viewportIndex) {
            ViewportBase viewport = Viewports[viewportIndex];
            Viewport xnaViewport = GetXnaViewport(viewportIndex);
            Rectangle xnaRect = GetXnaRectangle(viewportIndex);
            viewport.X = xnaRect.X;
            viewport.Y = xnaRect.Y;
            viewport.Width = xnaRect.Width;
            viewport.Height = xnaRect.Height;
            
            void resetBasicEffect() {
                basicEffect.Projection = viewport.GetViewportMatrix();
                basicEffect.View = viewport.GetCameraMatrix();
                basicEffect.World = Matrix.Identity;
                basicEffect.CurrentTechnique.Passes[0].Apply();
            };

            GlobalGraphics.GraphicsDevice.ScissorRectangle = xnaViewport.Bounds;
            GlobalGraphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;
            GlobalGraphics.GraphicsDevice.Viewport = xnaViewport;

            resetBasicEffect();

            viewport.DrawGrid();
            viewport.Render();

            resetBasicEffect();

            GlobalGraphics.GraphicsDevice.DepthStencilState = viewport is Viewport3D ? DepthStencilState.Default : DepthStencilState.None;
            GameMain.Instance.SelectedTool?.Render(viewport);

            GlobalGraphics.GraphicsDevice.Viewport = prevViewport;
        }

        private bool prevMouse1Down = false;
        private bool prevMouse2Down = false;
        private bool prevMouse3Down = false;
        private Keys[] prevKeysDown = Array.Empty<Keys>();
        private int prevScrollWheelValue = 0;
        private int focusedViewport = -1;
        
        public override void Update() {
            if (!DocumentManager.Documents.Contains(DocumentManager.CurrentDocument)) { return; }
            
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

            try {
                GameMain.Instance.SelectedTool?.Update();
                var doc = DocumentManager.CurrentDocument;
                if (doc is { LightmapTextureOutdated: true }) {
                    doc.LightmapTextureOutdated = false;
                }

                if (!selected) { return; }

                if (draggingCenter != DraggingMode.None) { return; }

                ViewportManager.Ctrl = keyboardState.IsKeyDown(Keys.LeftControl)
                                       || keyboardState.IsKeyDown(Keys.RightControl);
                ViewportManager.Shift = keyboardState.IsKeyDown(Keys.LeftShift)
                                        || keyboardState.IsKeyDown(Keys.RightShift);
                ViewportManager.Alt = keyboardState.IsKeyDown(Keys.LeftAlt)
                                      || keyboardState.IsKeyDown(Keys.RightAlt);

                foreach (var key in keysDown.Where(k => !prevKeysDown.Contains(k))) {
                    GameMain.Instance.SelectedTool?.KeyHit(new ViewportEvent() {
                        Handled = false,
                        KeyCode = key
                    });
                }

                foreach (var key in prevKeysDown.Where(k => !keysDown.Contains(k))) {
                    GameMain.Instance.SelectedTool?.KeyLift(new ViewportEvent() {
                        Handled = false,
                        KeyCode = key
                    });
                }

                var mousePos = mouseState.Position;

                for (int i = 0; i < Viewports.Length; i++) {
                    var viewport = Viewports[i];

                    bool mouseOver = (focusedViewport == -1 && GetXnaRectangle(i).Contains(mousePos))
                                     || focusedViewport == i;
                    if (mouseOver) {
                        if (!mouse1Down && prevMouse1Down) {
                            GameMain.Instance.SelectedTool?.MouseLifted(viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = mouseState.X - viewport.X,
                                Y = mouseState.Y - viewport.Y,
                                LastX = viewport.PrevMouseX,
                                LastY = viewport.PrevMouseY,
                            });
                            ViewportManager.MarkForRerender();
                            focusedViewport = -1;
                        }

                        GameMain.Instance.SelectedTool?.UpdateFrame(viewport,
                            new FrameInfo(0)); //TODO: fix FrameInfo

                        foreach (var key in keysDown.Where(k => !prevKeysDown.Contains(k))) {
                            GameMain.Instance.SelectedTool?.KeyHit(viewport, new ViewportEvent() {
                                Handled = false,
                                KeyCode = key
                            });
                            foreach (var tool in GameMain.Instance.ToolBarItems.Select(tbi => tbi.Tool)) {
                                tool.KeyHitBackground(viewport, new ViewportEvent() {
                                    Handled = false,
                                    KeyCode = key
                                });
                            }
                        }

                        foreach (var key in prevKeysDown.Where(k => !keysDown.Contains(k))) {
                            GameMain.Instance.SelectedTool?.KeyLift(viewport, new ViewportEvent() {
                                Handled = false,
                                KeyCode = key
                            });
                            foreach (var tool in GameMain.Instance.ToolBarItems.Select(tbi => tbi.Tool)) {
                                tool.KeyUpBackground(viewport, new ViewportEvent() {
                                    Handled = false,
                                    KeyCode = key
                                });
                            }
                        }

                        if (viewport is Viewport3D vp3d) {
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

                                ViewportManager.MarkForRerender();
                            }
                        }

                        int currMouseX = mouseState.X - viewport.X;
                        int currMouseY = mouseState.Y - viewport.Y;
                        if (viewport.PrevMouseX != currMouseX ||
                            viewport.PrevMouseY != currMouseY) {
                            var ev = new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewport.PrevMouseX,
                                LastY = viewport.PrevMouseY,
                            };
                            GameMain.Instance.SelectedTool?.MouseMove(viewport, ev);
                            foreach (var tool in GameMain.Instance.ToolBarItems.Select(tbi => tbi.Tool)) {
                                tool.MouseMoveBackground(viewport, ev);
                            }
                            ViewportManager.MarkForRerender();
                        }

                        if (mouse1Hit) {
                            GameMain.Instance.SelectedTool?.MouseClick(viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Left,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewport.PrevMouseX,
                                LastY = viewport.PrevMouseY,
                                Clicks = 1
                            });

                            focusedViewport = i;
                        }

                        if (mouse2Hit) {
                            GameMain.Instance.SelectedTool?.MouseClick(viewport, new ViewportEvent() {
                                Handled = false,
                                Button = MouseButtons.Right,
                                X = currMouseX,
                                Y = currMouseY,
                                LastX = viewport.PrevMouseX,
                                LastY = viewport.PrevMouseY,
                                Clicks = 1
                            });
                        }

                        if (mouse3Down) {
                            if (viewport is Viewport2D vp) {
                                ViewportManager.MarkForRerender();
                                vp.Position -= new DataStructures.Geometric.Vector3(
                                    (decimal)(currMouseX - vp.PrevMouseX) / vp.Zoom,
                                    -(decimal)(currMouseY - vp.PrevMouseY) / vp.Zoom, 0m);
                            }
                        }


                        if (scrollWheelValue != prevScrollWheelValue) {
                            if (viewport is Viewport2D vp) {
                                var pos0 = vp.ScreenToWorld(new Point(mouseState.X - viewport.X,
                                    mouseState.Y - viewport.Y));
                                decimal scrollWheelDiff = (scrollWheelValue - prevScrollWheelValue) * 0.001m;
                                if (scrollWheelDiff > 0m) {
                                    vp.Zoom *= 1.0m + scrollWheelDiff;
                                } else {
                                    vp.Zoom /= 1.0m - scrollWheelDiff;
                                }

                                var pos1 = vp.ScreenToWorld(new Point(mouseState.X - viewport.X,
                                    mouseState.Y - viewport.Y));
                                vp.Position -=
                                    new DataStructures.Geometric.Vector3(pos1.X - pos0.X, pos0.Y - pos1.Y, 0m);
                                ViewportManager.MarkForRerender();
                            }
                        }
                    }

                    //Reset the mouse state since the tool update methods can change it!
                    mouseState = Mouse.GetState();
                    viewport.PrevMouseOver = mouseOver;
                    viewport.PrevMouseX = mouseState.X - viewport.X;
                    viewport.PrevMouseY = mouseState.Y - viewport.Y;
                }
            } finally {
                prevMouse1Down = mouse1Down;
                prevMouse2Down = mouse2Down;
                prevMouse3Down = mouse3Down;
                prevKeysDown = keysDown;
                prevScrollWheelValue = scrollWheelValue;
            }
        }

        private (Rectangle HorizontalLine, Rectangle VerticalLine) GetDrawLines() {
            int centerX = WindowRectangle.Left + (int)(ViewportCenter.X * WindowRectangle.Width);
            int centerY = WindowRectangle.Top + (int)(ViewportCenter.Y * WindowRectangle.Height);
            var horizontalLine = new Rectangle(
                WindowRectangle.Left, centerY - (viewportGap - 1),
                WindowRectangle.Width, (viewportGap - 1) * 2);
            var verticalLine = new Rectangle(
                centerX - (viewportGap - 1), WindowRectangle.Top,
                (viewportGap - 1) * 2, WindowRectangle.Height);
            return (horizontalLine, verticalLine);
        }

        private (Rectangle HorizontalLine, Rectangle VerticalLine) GetHoverLines() {
            var (horizontalLine, verticalLine) = GetDrawLines();
            horizontalLine.Y -= 1; horizontalLine.Height += 2;
            verticalLine.X -= 1; verticalLine.Width += 2;
            return (horizontalLine, verticalLine);
        }
        
        protected override void ImGuiLayout(out bool shouldBeOpen) {
            shouldBeOpen = true;
            
            Num.Vector2 pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
            Num.Vector2 siz = ImGui.GetWindowSize() - ImGui.GetCursorPos() * 1.5f;
            Rectangle windowRectangle = new Rectangle((int)pos.X, (int)pos.Y, (int)siz.X, (int)siz.Y);
            if (!WindowRectangle.Equals(windowRectangle)) {
                WindowRectangle = windowRectangle;
                ResetRenderTarget();
            }
            WindowRectangle = windowRectangle;
            if (ImGui.BeginChildFrame(1, new Num.Vector2(WindowRectangle.Size.X, WindowRectangle.Size.Y), flags) && RenderTargetImGuiPtr != IntPtr.Zero && Open) {
                ImGui.SetCursorPos(Num.Vector2.Zero);
                var cursorPos = ImGui.GetCursorPos();
                ImGui.Image(RenderTargetImGuiPtr, new Num.Vector2(WindowRectangle.Size.X, WindowRectangle.Size.Y));

                const uint unhoveredColor = 0xff494040;
                const uint hoveredColor = 0xff995318;

                var (horizontalLine, verticalLine) = GetDrawLines();
                var (horizontalLineHover, verticalLineHover) = GetHoverLines();
                
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                for (int i = 0; i < Viewports.Length; i++) {
                    var xnaRect = GetXnaRectangle(i);
                    Num.Vector2 clipRectMin = new Num.Vector2(
                        xnaRect.Left,
                        xnaRect.Top);
                    Num.Vector2 clipRectMax = new Num.Vector2(
                        xnaRect.Right,
                        xnaRect.Bottom);
                    ImGui.PushClipRect(clipRectMin, clipRectMax, intersect_with_current_clip_rect: true);

                    var topLeft = xnaRect.Location;
                    var textPos = new Num.Vector2(topLeft.X + 5, topLeft.Y + 5);
                    using (new AggregateDisposable(
                               new ColorPush(ImGuiCol.Button, Color.Black),
                               new ColorPush(ImGuiCol.ButtonActive, Color.DarkGray),
                               new ColorPush(ImGuiCol.ButtonHovered, Color.Gray))) {
                        ImGui.SetCursorScreenPos(textPos);
                        if (ImGui.Button($"{Viewports[i].GetViewType()}##viewType{i}")) {
                            ImGui.OpenPopup($"viewTypePopup{i}");
                        }

                        if (ImGui.BeginPopup($"viewTypePopup{i}")) {
                            foreach (Viewport3D.ViewType viewType in Enum.GetValues<Viewport3D.ViewType>()) {
                                if (ImGui.Selectable($"3D {viewType}")) {
                                    Viewports[i] = new Viewport3D(viewType);
                                }
                            }
                                    
                            ImGui.Separator();
                                    
                            foreach (Viewport2D.ViewDirection viewType in Enum.GetValues<Viewport2D.ViewDirection>()) {
                                if (ImGui.Selectable($"2D {viewType}")) {
                                    Viewports[i] = new Viewport2D(viewType);
                                }
                            }
                            ImGui.EndPopup();
                        }
                    }
                    ImGui.PopClipRect();
                }

                void addRect(Rectangle rect, uint color)
                    => drawList.AddRect(new Num.Vector2(rect.Left, rect.Top) + cursorPos,
                        new Num.Vector2(rect.Right, rect.Bottom) + cursorPos, color);
                addRect(horizontalLine, unhoveredColor);
                addRect(verticalLine, unhoveredColor);

                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                    draggingCenter = DraggingMode.None;
                }
                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows)) {
                    bool wasDragging = draggingCenter != DraggingMode.None;
                    var mousePosImGui = ImGui.GetMousePos();
                    var mousePos = new Point((int)mousePosImGui.X, (int)mousePosImGui.Y);

                    void handleHover(Rectangle drawRect, Rectangle hoverRect, DraggingMode dragFlag) {
                        if ((hoverRect.Contains(mousePos) && !wasDragging && focusedViewport < 0)
                            || draggingCenter.HasFlag(dragFlag)) {
                            addRect(drawRect, hoveredColor);
                            if (!wasDragging && ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                                draggingCenter |= dragFlag;
                            }
                        }
                    }
                    handleHover(horizontalLine, horizontalLineHover, DraggingMode.Horizontal);
                    handleHover(verticalLine, verticalLineHover, DraggingMode.Vertical);

                    var viewportCenter = ViewportCenter;
                    bool forceRerender = false;
                    if (draggingCenter.HasFlag(DraggingMode.Horizontal)) {
                        viewportCenter.Y = (float)(mousePos.Y - WindowRectangle.Top) / (float)WindowRectangle.Height;
                        forceRerender = true;
                    }
                    if (draggingCenter.HasFlag(DraggingMode.Vertical)) {
                        viewportCenter.X = (float)(mousePos.X - WindowRectangle.Left) / (float)WindowRectangle.Width;
                        forceRerender = true;
                    }

                    viewportCenter.X = Math.Clamp(
                        viewportCenter.X,
                        5.0f / WindowRectangle.Width,
                        (WindowRectangle.Width - 5.0f) / WindowRectangle.Width);
                    viewportCenter.Y = Math.Clamp(
                        viewportCenter.Y,
                        5.0f / WindowRectangle.Height,
                        (WindowRectangle.Height - 5.0f) / WindowRectangle.Height);
                    ViewportCenter = viewportCenter;
                    if (forceRerender) { ViewportManager.MarkForRerender(); }
                }
                
                
                ImGui.EndChildFrame();
            }
            selected = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);
        }

        public override void Dispose() {
            BasicEffect.Dispose();
            if (RenderTargetImGuiPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(RenderTargetImGuiPtr);
            }
            RenderTarget?.Dispose();
        }
    }
}

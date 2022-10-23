using System;
using System.Linq;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using ImGuiNET;
using ImGuizmoNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CBRE.Editor.Popup {
    public partial class ViewportWindow : DockableWindow {
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
            if (FullscreenViewport == viewportIndex) {
                return new Viewport(WindowRectangle.X, WindowRectangle.Y, WindowRectangle.Width, WindowRectangle.Height);
            } else if (FullscreenViewport != -1) {
                return new Viewport(0, 0, 0, 0);
            }
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
            if (FullscreenViewport == -1)
                return new Rectangle(xnaViewport.X + WindowRectangle.X, xnaViewport.Y + WindowRectangle.Y, xnaViewport.Width, xnaViewport.Height);
            return new Rectangle(xnaViewport.X, xnaViewport.Y, xnaViewport.Width, xnaViewport.Height);
        }
        
        public RenderTarget2D[] RenderTarget { get; private set; }
        public IntPtr[] RenderTargetImGuiPtr { get; private set; }
        public bool[] RenderTargetSelected { get; private set; }
        public BasicEffect BasicEffect { get; private set; }
        public string Name { get; private set; }
        private bool selected = false;
        public int FullscreenViewport = -1;

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
            RenderTarget = new RenderTarget2D[Viewports.Length];
            RenderTargetImGuiPtr = new IntPtr[Viewports.Length];
            RenderTargetSelected = new bool[Viewports.Length];
        }

        public void ResetRenderTarget(int index) {
            if (WindowRectangle.Width <= 0 && WindowRectangle.Height <= 0) { return; }
            if (RenderTargetImGuiPtr[index] != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(RenderTargetImGuiPtr[index]);
                RenderTargetImGuiPtr[index] = IntPtr.Zero;
            }
            RenderTarget[index]?.Dispose();
            RenderTarget[index] = new RenderTarget2D(GlobalGraphics.GraphicsDevice, Math.Max(WindowRectangle.Width, 4), Math.Max(WindowRectangle.Height, 4), false, SurfaceFormat.Color, DepthFormat.Depth24);

            GlobalGraphics.GraphicsDevice.SetRenderTarget(RenderTarget[index]);
            GlobalGraphics.GraphicsDevice.Clear(Color.Black);
            Render(index);
            /*for (int i = 0; i < Viewports.Length; i++) {
                Render(i);
            }*/

            basicEffect.Projection =
                Matrix.CreateTranslation(-RenderTarget[index].Width / 2, -RenderTarget[index].Height / 2, 0.0f)
                * Matrix.CreateOrthographic(RenderTarget[index].Width, RenderTarget[index].Height, -1.0f, 1.0f);
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;
            basicEffect.CurrentTechnique.Passes[0].Apply();

            GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
            RenderTargetImGuiPtr[index] = GlobalGraphics.ImGuiRenderer.BindTexture(RenderTarget[index]);
        }
        
        private void Render(int viewportIndex) {
            ViewportBase viewport = Viewports[viewportIndex];
            // return new Viewport(0, 0, WindowRectangle.Width, WindowRectangle.Height);
            Viewport xnaViewport = new Viewport(0, 0, WindowRectangle.Width, WindowRectangle.Height);
            // Viewport xnaViewport = GetXnaViewport(viewportIndex);
            // Rectangle xnaRect = new Rectangle(xnaViewport.X + WindowRectangle.X, xnaViewport.Y + WindowRectangle.Y, xnaViewport.Width, xnaViewport.Height);
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
                    if (FullscreenViewport != -1 && FullscreenViewport != i) continue;
                    UpdateSubView(i);
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
                for (int i = 0; i < Viewports.Length; i++) {
                    ResetRenderTarget(i);
                }
            }
            WindowRectangle = windowRectangle;
            for (int i = 0; i < Viewports.Length; i++) {
                RenderTargetSelected[i] = false;
            }
            // ImGui.SetCursorPos(Num.Vector2.Zero);
            if (ImGui.BeginChild((Viewports.Length + 1).ToString(), new Num.Vector2(WindowRectangle.Size.X, WindowRectangle.Size.Y), false, flags) && Open) {
                var cursorPos = ImGui.GetCursorPos();

                const uint unhoveredColor = 0xff494040;
                const uint hoveredColor = 0xff995318;

                var (horizontalLine, verticalLine) = GetDrawLines();
                var (horizontalLineHover, verticalLineHover) = GetHoverLines();
                
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                for (int i = 0; i < Viewports.Length; i++) {
                    if (FullscreenViewport != -1 && FullscreenViewport != i) continue;
                    var xnaRect = GetXnaRectangle(i);
                    Num.Vector2 clipRectMin = new Num.Vector2(
                        xnaRect.Left,
                        xnaRect.Top);
                    Num.Vector2 clipRectMax = new Num.Vector2(
                        xnaRect.Right,
                        xnaRect.Bottom);
                    RenderSubView(i, true);
                    ImGui.PushClipRect(clipRectMin, clipRectMax, intersect_with_current_clip_rect: true);

                    var objectRenderer = DocumentManager.CurrentDocument.ObjectRenderer;

                    if (Viewports[i] is Viewport3D vp3d) {
                        try {
                            Matrix matView = Matrix.CreateLookAt(-vp3d.Camera.Direction.ToXna() * 3f, vp3d.Camera.Direction.ToXna(), vp3d.Camera.GetUp().ToXna());
                            float[] view = Viewports[i].GetCameraMatrix().ToCbre().Values.Select(x => (float)x).ToArray();
                            float[] viewC = matView.ToCbre().Values.Select(x => (float)x).ToArray();
                            float[] proj = Viewports[i].GetViewportMatrix().ToCbre().Values.Select(x => (float)x).ToArray();
                            Matrix matWorld = Matrix.Identity;
                            float[] world = matWorld.ToCbre().Values.Select(x => (float)x).ToArray();
                            float[][] worldC = new float[][] { matWorld.ToCbre().Values.Select(x => (float)x).ToArray() };

                            var xnaRect2 = xnaRect;
                            xnaRect2.Size = new Point(50, 50);
                            xnaRect2.Location = new Point(xnaRect.Right - xnaRect2.Size.X, xnaRect.Top);

                            ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());
                            ImGuizmo.SetRect(xnaRect2.X, xnaRect2.Y, xnaRect2.Width, xnaRect2.Height);
                            ImGuizmo.DrawCubes(ref viewC[0], ref proj[0], ref worldC[0][0], 1);
                            /*ImGuizmo.SetRect(xnaRect.X, xnaRect.Y, xnaRect.Width, xnaRect.Height);
                            ImGuizmo.Manipulate(ref view[0], ref proj[0], OPERATION.ROTATE, MODE.LOCAL, ref world[0]);
                            if (ImGuizmo.IsOver() || ImGuizmo.IsUsing()) {
                                RenderTargetSelected[i] = false;
                            }*/
                        } catch { }
                    }

                    var topLeft = xnaRect.Location;
                    var textPos = new Num.Vector2(topLeft.X + 5, topLeft.Y + 5);
                    using (new AggregateDisposable(
                               new ColorPush(ImGuiCol.Button, Color.Black),
                               new ColorPush(ImGuiCol.ButtonActive, Color.DarkGray),
                               new ColorPush(ImGuiCol.ButtonHovered, Color.Gray))) {
                        ImGui.SetCursorScreenPos(textPos);

                        if (ImGui.Button($"{Viewports[i].GetViewType()}##viewType{i}")) {
                            RenderTargetSelected[i] = false;
                            ImGui.OpenPopup($"viewTypePopup{i}");
                        }
                        if (ImGui.IsItemHovered()) {
                            RenderTargetSelected[i] = false;
                        }

                        if (ImGui.BeginPopup($"viewTypePopup{i}")) {
                            RenderTargetSelected[i] = false;

                            if (ImGui.Selectable("Toggle Fullscreen")) {
                                if (FullscreenViewport == -1)
                                    FullscreenViewport = i;
                                else
                                    FullscreenViewport = -1;
                            }

                            ImGui.Separator();

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
                    
                    foreach (var tool in ToolManager.Tools)
                    {
                        tool.ViewportUi(Viewports[i]);
                    }
                    
                    ImGui.PopClipRect();
                }

                if (FullscreenViewport == -1) {
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
                }
                selected = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);
                ImGui.EndChild();
            } else {
                selected = false;
                for (int i = 0; i < Viewports.Length; i++) {
                    RenderTargetSelected[i] = false;
                }
            }
        }

        public override void Dispose() {
            BasicEffect.Dispose();
            for (int i = 0; i < Viewports.Length; i++) {
                if (RenderTargetImGuiPtr[i] != IntPtr.Zero) {
                    GlobalGraphics.ImGuiRenderer.UnbindTexture(RenderTargetImGuiPtr[i]);
                }
                RenderTarget[i]?.Dispose();
            }
        }
    }
}

using System;
using System.Drawing;
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
    public class ViewportWindow : WindowUI
    {
        public ViewportBase Viewport { get; private set; }
        public Rectangle View { get; private set; }
        public RenderTarget2D RenderTarget { get; private set; }
        public IntPtr RenderTargetPtr { get; private set; }
        public BasicEffect BasicEffect { get; private set; }
        public string Name { get; private set; }
        private bool selected = false;

        public ViewportWindow(ViewportBase view) : base("") {
            Viewport = view;
            BasicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            Name = Guid.NewGuid().ToString();
        }

        public ViewportWindow(int viewid) : base("") {
            Viewport = ViewportManager.Viewports[viewid];
            BasicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            Name = viewid.ToString();
        }

        public void ResetRenderTarget() {
            if (View.Width <= 0 && View.Height <= 0) { return; }
            if (RenderTargetPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(RenderTargetPtr);
                RenderTargetPtr = IntPtr.Zero;
            }
            RenderTarget?.Dispose();
            RenderTarget = new RenderTarget2D(GlobalGraphics.GraphicsDevice, Math.Max(View.Width, 4), Math.Max(View.Height, 4), false, SurfaceFormat.Color, DepthFormat.Depth24);

            ViewportManager.Render(Viewport, new Viewport(0, 0, RenderTarget.Width, RenderTarget.Height), RenderTarget);
            RenderTargetPtr = GlobalGraphics.ImGuiRenderer.BindTexture(RenderTarget);
        }

        public virtual bool IsOverAndOpen(MouseState mouseState) {
            return (selected && mouseState.X >= -View.Left && mouseState.X <= View.Right && mouseState.Y >= -View.Top && mouseState.Y <= View.Bottom);
        }

        protected override bool ImGuiLayout() {
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;
            if (ImGui.Begin(Name, ref open, flags)) {
                Num.Vector2 pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
                Num.Vector2 siz = ImGui.GetWindowSize() - ImGui.GetCursorPos() * 1.5f;
                Rectangle tmpview = new Rectangle((int)pos.X, (int)pos.Y, (int)siz.X, (int)siz.Y);
                if (!View.Equals(tmpview)) {
                    View = tmpview;
                    ResetRenderTarget();
                }
                View = tmpview;
                if (ImGui.BeginChildFrame(1, new Num.Vector2(View.Size.X, View.Size.Y), flags) && RenderTargetPtr != IntPtr.Zero && open) {
                    ImGui.Image(RenderTargetPtr, new Num.Vector2(View.Size.X, View.Size.Y));
                    ImGui.EndChildFrame();
                }
                selected = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);
                ImGui.End();
            }
            return open;
        }

        public override void Close() {
            base.Close();
            BasicEffect.Dispose();
            if (RenderTargetPtr != IntPtr.Zero) {
                GlobalGraphics.ImGuiRenderer.UnbindTexture(RenderTargetPtr);
            }
            RenderTarget?.Dispose();
        }
    }
}

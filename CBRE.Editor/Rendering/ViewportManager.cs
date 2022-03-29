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

    public struct ViewportEvent {
        public bool Handled { get; set; }

        // Key
        //public Keys Modifiers { get; set; }
        public bool Control { get; init; }
        public bool Shift { get; init; }
        public bool Alt { get; init; }
        
        public Keys KeyCode { get; init; }
        public int KeyValue { get; init; }
        public char KeyChar { get; init; }

        // Mouse
        public MouseButtons Button { get; init; }
        public int Clicks { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Delta { get; init; }

        public Vector3 Location => new Vector3(X, Y, 0);

        // Mouse movement
        public int LastX { get; init; }
        public int LastY { get; init; }

        public int DeltaX => X - LastX;
        public int DeltaY => Y - LastY;

        // Click and drag
        public bool MouseOver { get; init; }
        public bool Dragging { get; init; }
        public int StartX { get; init; }
        public int StartY { get; init; }

        // 2D Camera
        public Vector3 CameraPosition { get; init; }
        public decimal CameraZoom { get; init; }
    }

    #warning TODO: This class has cancer
    static class ViewportManager {
        public static ViewportBase[] Viewports { get; private set; } = new ViewportBase[4];

        public static bool Ctrl { get; set; }
        public static bool Shift { get; set; }
        public static bool Alt { get; set; }


        public static Rectangle VPRect { get; set; } = new Rectangle(0, 0, 640, 480);

        public static void Init() {
            Viewports[0] = new Viewport3D(Viewport3D.ViewType.Textured);
            Viewports[1] = new Viewport2D(Viewport2D.ViewDirection.Top);
            Viewports[2] = new Viewport2D(Viewport2D.ViewDirection.Side);
            Viewports[3] = new Viewport2D(Viewport2D.ViewDirection.Front);

            AsyncTexture.LoadCallback = TextureLoadCallback;
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

        public static void RenderIfNecessary() {
            if (!shouldRerender) { return; }
            Render();
        }

        public static void Render() {
            shouldRerender = false;
            IEnumerable<ViewportWindow> vpWindows
                = GameMain.Instance.Dockables.Where(d => d is ViewportWindow).Cast<ViewportWindow>();
            foreach (var viewportWindow in vpWindows)
            {
                viewportWindow.ResetRenderTarget();
            }
        }
    }
}

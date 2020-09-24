using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CBRE.Graphics {
    public static class GlobalGraphics {
        public static GraphicsDevice GraphicsDevice { get; private set; }
        public static GameWindow Window { get; private set; }
        public static ImGuiRenderer ImGuiRenderer { get; private set; }

        public static MouseCursor RotateCursor { get; private set; }


        public static void Set(GraphicsDevice gfxDev, GameWindow window, ImGuiRenderer imGuiRenderer) {
            GraphicsDevice = gfxDev;
            Window = window;
            ImGuiRenderer = imGuiRenderer;
        }
    }
}

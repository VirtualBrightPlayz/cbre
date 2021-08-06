using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace CBRE.Graphics {
    public static class GlobalGraphics {
        public static GraphicsDevice GraphicsDevice { get; private set; }
        public static Sdl2Window Window { get; private set; }
        public static ImGuiRenderer ImGuiRenderer { get; private set; }

        public static void Set(GraphicsDevice gfxDev, Sdl2Window window, ImGuiRenderer imGuiRenderer) {
            GraphicsDevice = gfxDev;
            Window = window;
            ImGuiRenderer = imGuiRenderer;
        }
    }
}

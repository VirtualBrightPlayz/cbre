using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Graphics {
    public static class GlobalGraphics {
        public static GraphicsDevice GraphicsDevice { get; private set; } 
        public static ImGuiRenderer ImGuiRenderer { get; private set; }

        public static void Set(GraphicsDevice gfxDev, ImGuiRenderer imGuiRenderer) {
            GraphicsDevice = gfxDev;
            ImGuiRenderer = imGuiRenderer;
        }
    }
}

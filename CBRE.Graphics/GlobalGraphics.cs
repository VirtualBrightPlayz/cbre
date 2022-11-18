using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace CBRE.Graphics {
    public static class GlobalGraphics {
        public static GraphicsDevice GraphicsDevice { get; private set; }
        public static GameWindow Window { get; private set; }
        public static ImGuiRenderer ImGuiRenderer { get; private set; }

        public static MouseCursor RotateCursor { get; private set; }

        public static class SelectedColors {
            public static readonly Num.Vector4 Button = new Num.Vector4(0.3f, 0.6f, 0.7f, 1.0f);
            public static readonly Num.Vector4 ButtonActive = new Num.Vector4(0.15f, 0.3f, 0.4f, 1.0f);
            public static readonly Num.Vector4 ButtonHovered = new Num.Vector4(0.45f, 0.9f, 1.0f, 1.0f);
        }


        public static void Set(GraphicsDevice gfxDev, GameWindow window, ImGuiRenderer imGuiRenderer) {
            GraphicsDevice = gfxDev;
            Window = window;
            ImGuiRenderer = imGuiRenderer;
        }
        
        
        public static Effect LoadEffect(string filename) {
            using var fs = File.OpenRead(filename);
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, bytes.Length);
            return new Effect(GraphicsDevice, bytes);
        }
    }
}

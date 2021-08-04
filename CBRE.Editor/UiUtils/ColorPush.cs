using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace CBRE.Editor {
    struct ColorPush : IDisposable {
        public ColorPush(ImGuiCol element, Num.Vector4 color) {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, color);
        }

        public ColorPush(ImGuiCol element, ImColor color) : this(element, color.Value) { }

        public ColorPush(ImGuiCol element, Color color) : this(element, new Num.Vector4(
            (float)color.R / 255f,
            (float)color.R / 255f,
            (float)color.R / 255f,
            (float)color.R / 255f)) { }
        
        public ColorPush(ImGuiCol element, System.Drawing.Color color) : this(element, new Num.Vector4(
            (float)color.R / 255f,
            (float)color.R / 255f,
            (float)color.R / 255f,
            (float)color.R / 255f)) { }
        
        public void Dispose() {
            ImGui.PopStyleColor();
        }
    }
}

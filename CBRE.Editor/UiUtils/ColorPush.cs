using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace CBRE.Editor {
    readonly struct ColorPush : IDisposable {
        public static AggregateDisposable RedButton()
            => new AggregateDisposable(
                new ColorPush(ImGuiCol.Button, Color.DarkRed),
                new ColorPush(ImGuiCol.ButtonActive, Color.DarkRed),
                new ColorPush(ImGuiCol.ButtonHovered, Color.Red));
        
        private readonly bool pushed;
        
        public ColorPush(ImGuiCol element, Num.Vector4? color) {
            if (color.HasValue) { ImGui.PushStyleColor(element, color.Value); }
            pushed = color.HasValue;
        }

        public ColorPush(ImGuiCol element, ImColor? color) : this(element, color?.Value) { }

        public ColorPush(ImGuiCol element, Color color) : this(element, new Num.Vector4(
            (float)color.R / 255f,
            (float)color.G / 255f,
            (float)color.B / 255f,
            (float)color.A / 255f)) { }
        
        public ColorPush(ImGuiCol element, System.Drawing.Color color) : this(element, new Num.Vector4(
            (float)color.R / 255f,
            (float)color.G / 255f,
            (float)color.B / 255f,
            (float)color.A / 255f)) { }
        
        public void Dispose() {
            if (pushed) { ImGui.PopStyleColor(); }
        }
    }
}

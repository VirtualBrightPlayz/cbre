using System;
using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace CBRE.Editor {
    readonly struct ColorPush : IDisposable {
        public static IDisposable RedButton()
            => new AggregateDisposable(
                new ColorPush(ImGuiCol.Button, Color.DarkRed),
                new ColorPush(ImGuiCol.ButtonActive, Color.DarkRed),
                new ColorPush(ImGuiCol.ButtonHovered, Color.Red));

        public static IDisposable SelectedButton()
            => new AggregateDisposable(
                new ColorPush(ImGuiCol.Button, GlobalGraphics.SelectedColors.Button),
                new ColorPush(ImGuiCol.ButtonActive, GlobalGraphics.SelectedColors.ButtonActive),
                new ColorPush(ImGuiCol.ButtonHovered, GlobalGraphics.SelectedColors.ButtonHovered));

        public static IDisposable Nil()
            => new ColorPush(ImGuiCol.Button, (Num.Vector4?)null);
        
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

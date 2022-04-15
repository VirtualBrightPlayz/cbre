using System;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Documents;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class ExportPopup : PopupUI {
        protected override bool canBeDefocused => false;
        protected override bool hasOkButton => false;

        private readonly Document document;
        private int textureDims;
        private float downscaleFactor;
        private int blurRadius;
        private Color ambientLightColor;

        private readonly Lightmapper lightmapper;

        public ExportPopup(Document document) : base("Export / Compile") {
            this.document = document;
            textureDims = LightmapConfig.TextureDims;
            downscaleFactor = LightmapConfig.DownscaleFactor;
            ambientLightColor = new Color(
                (byte)LightmapConfig.AmbientColorR,
                (byte)LightmapConfig.AmbientColorG,
                (byte)LightmapConfig.AmbientColorB);
            blurRadius = LightmapConfig.BlurRadius;
            lightmapper = new Lightmapper(document);
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            ImGui.SetWindowSize(new Num.Vector2(300,200), ImGuiCond.FirstUseEver);
            shouldBeOpen = true;

            ImGui.PushItemWidth(100.0f);
            
            ImGui.Text("Lightmap dimensions");
            ImGui.SameLine();
            ImGui.InputInt("##lmDim", ref textureDims);
            
            float quality = 100.0f / downscaleFactor;
            ImGui.Text("Quality");
            ImGui.SameLine();
            ImGui.InputFloat("##quality", ref quality);
            quality = MathF.Max(quality, 0.01f);
            downscaleFactor = MathF.Round(100.0f / quality);
            ImGui.SameLine();
            ImGui.Text("    Downscale factor");
            ImGui.SameLine();
            ImGui.InputFloat("##downscaleFactor", ref downscaleFactor);
            downscaleFactor = MathF.Max(1.0f, MathF.Abs(downscaleFactor));
            
            ImGui.Text("Blur radius");
            ImGui.SameLine();
            ImGui.InputInt("##blurRadius", ref blurRadius);
            
            ImGui.PopItemWidth();

            ImGui.PushItemWidth(200.0f);
            
            ImGui.Text("Ambient light color");
            Num.Vector3 ambientLight = new(ambientLightColor.R / 255.0f, ambientLightColor.G / 255.0f, ambientLightColor.B / 255.0f);
            ImGui.ColorPicker3("##ambientLight", ref ambientLight);
            ambientLightColor = new Color(ambientLight.X, ambientLight.Y, ambientLight.Z);
            
            ImGui.PopItemWidth();
            
            ImGui.PushItemWidth(200.0f);
            if (ImGui.Button("Render")) {
                LightmapConfig.DownscaleFactor = downscaleFactor;
                LightmapConfig.BlurRadius = blurRadius;
                LightmapConfig.TextureDims = textureDims;
                LightmapConfig.AmbientColorR = ambientLightColor.R;
                LightmapConfig.AmbientColorG = ambientLightColor.G;
                LightmapConfig.AmbientColorB = ambientLightColor.B;
                lightmapper.Render();
            }
            ImGui.PopItemWidth();
        }
    }
}

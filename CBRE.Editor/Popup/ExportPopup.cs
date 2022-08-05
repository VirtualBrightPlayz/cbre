using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Compiling.Lightmap.Legacy;
using CBRE.Editor.Documents;
using CBRE.Graphics;
using CBRE.Providers.Map;
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
        private Num.Vector3 ambientLightNormal;
        public int planeMargin;
        public bool bakeModels;
        public bool bakeModelLightmaps;
        public bool computeShadows;
        public float bakeGamma;

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
            planeMargin = LightmapConfig.PlaneMargin;
            bakeModels = LightmapConfig.BakeModels;
            bakeModelLightmaps = LightmapConfig.BakeModelLightmaps;
            computeShadows = LightmapConfig.ComputeShadows;
            bakeGamma = LightmapConfig.BakeGamma;
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

            ImGui.Text("Plane margin");
            ImGui.SameLine();
            ImGui.InputInt("##planeMargin", ref planeMargin);

            ImGui.Text("Bake models");
            ImGui.SameLine();
            ImGui.Checkbox("##bakeModels", ref bakeModels);

            ImGui.Text("Bake models with lightmaps");
            ImGui.SameLine();
            ImGui.Checkbox("##bakeModelLightmaps", ref bakeModelLightmaps);

            ImGui.Text("Compute shadows");
            ImGui.SameLine();
            ImGui.Checkbox("##computeShadows", ref computeShadows);

            ImGui.Text("Bake Gamma");
            ImGui.SameLine();
            ImGui.InputFloat("##bakeGamma", ref bakeGamma);
            
            ImGui.PopItemWidth();

            ImGui.PushItemWidth(200.0f);
            
            ImGui.Text("Ambient light color");
            Num.Vector3 ambientLight = new(ambientLightColor.R / 255.0f, ambientLightColor.G / 255.0f, ambientLightColor.B / 255.0f);
            ImGui.ColorPicker3("##ambientLight", ref ambientLight);
            ambientLightColor = new Color(ambientLight.X, ambientLight.Y, ambientLight.Z);

            ImGui.Text("Ambient light normal");
            Num.Vector3 ambientNormal = new(ambientLightNormal.X, ambientLightNormal.Y, ambientLightNormal.Z);
            ImGui.InputFloat3("##ambientLightNormal", ref ambientNormal);
            ambientLightNormal = ambientNormal;
            
            ImGui.PopItemWidth();
            
            ImGui.PushItemWidth(200.0f);

            ImGui.Separator();
            ImGui.Text("Modern Lightmapper (Supports limited features)");
            if (ImGui.Button("Render##new")) {
                LightmapConfig.DownscaleFactor = downscaleFactor;
                LightmapConfig.BlurRadius = blurRadius;
                LightmapConfig.TextureDims = textureDims;
                LightmapConfig.AmbientColorR = ambientLightColor.R;
                LightmapConfig.AmbientColorG = ambientLightColor.G;
                LightmapConfig.AmbientColorB = ambientLightColor.B;
                LightmapConfig.AmbientNormalX = ambientLightNormal.X;
                LightmapConfig.AmbientNormalY = ambientLightNormal.Y;
                LightmapConfig.AmbientNormalZ = ambientLightNormal.Z;
                LightmapConfig.PlaneMargin = planeMargin;
                LightmapConfig.BakeModels = bakeModels;
                LightmapConfig.BakeModelLightmaps = bakeModelLightmaps;
                LightmapConfig.ComputeShadows = computeShadows;
                LightmapConfig.BakeGamma = bakeGamma;
                Task.Run(async () => {
                    try {
                        await lightmapper.RenderShadowMapped();
                    } catch (Exception e) {
                        Mediator.Publish(EditorMediator.CompileFailed, document);
                        Logging.Logger.ShowException(e);
                    }
                });
            }

            if (ImGui.Button("Export .rmesh##new")) {
                var result = NativeFileDialog.SaveDialog.Open("rmesh", Directory.GetCurrentDirectory(), out string path);
                if (result == NativeFileDialog.Result.Okay) {
                    if (document.MGLightmaps == null || document.MGLightmaps.Count == 0) {
                        GameMain.Instance.Popups.Add(new ConfirmPopup("Un-rendered map", "There is no lightmap detected, exporting will be done without lightmaps", new ImColor() { Value = new Num.Vector4(0.75f, 0f, 0f, 1f) }) {
                            Buttons = new [] {
                                new ConfirmPopup.Button("Export anyways", () => RMeshProvider.SaveToFile(path, document.Map, null, null, false)),
                                new ConfirmPopup.Button("Don't export", () => { }),
                            }.ToImmutableArray(),
                        });
                    }
                    else {
                        RMeshProvider.SaveToFile(path, document.Map, document.MGLightmaps.ToArray(), null, false);
                    }
                }
            }

            ImGui.Separator();
            ImGui.Text("Legacy Lightmapper (Supports all features)");
            if (LegacyLightmapper.FaceRenderThreads == null || LegacyLightmapper.FaceRenderThreads.Count == 0) {
                if (ImGui.Button("Render##legacy")) {
                    LightmapConfig.DownscaleFactor = downscaleFactor;
                    LightmapConfig.BlurRadius = blurRadius;
                    LightmapConfig.TextureDims = textureDims;
                    LightmapConfig.AmbientColorR = ambientLightColor.R;
                    LightmapConfig.AmbientColorG = ambientLightColor.G;
                    LightmapConfig.AmbientColorB = ambientLightColor.B;
                    LightmapConfig.AmbientNormalX = ambientLightNormal.X;
                    LightmapConfig.AmbientNormalY = ambientLightNormal.Y;
                    LightmapConfig.AmbientNormalZ = ambientLightNormal.Z;
                    LightmapConfig.PlaneMargin = planeMargin;
                    LightmapConfig.BakeModels = bakeModels;
                    LightmapConfig.BakeModelLightmaps = bakeModelLightmaps;
                    LightmapConfig.ComputeShadows = computeShadows;
                    LightmapConfig.BakeGamma = bakeGamma;
                    // lightmapper.RenderShadowMapped();
                    // lightmapper.RenderRayTest();
                    Task.Run(() => {
                        try {
                            LegacyLightmapper.Render(document, out _, out _, LightmapConfig.BakeModels);
                            // document.ObjectRenderer.MarkDirty();
                        } catch (Exception e) {
                            Mediator.Publish(EditorMediator.CompileFailed, document);
                            GameMain.Instance.PostDrawActions.Enqueue(() => {
                                Logging.Logger.ShowException(e);
                            });
                        }
                    });
                }

                if (ImGui.Button("Export .rmesh##legacy")) {
                    var result = NativeFileDialog.SaveDialog.Open("rmesh", Directory.GetCurrentDirectory(), out string path);
                    if (result == NativeFileDialog.Result.Okay) {
                        if (document.MGLightmaps == null || document.MGLightmaps.Count == 0) {
                            // using (ColorPush.RedButton()) {
                                GameMain.Instance.Popups.Add(new ConfirmPopup("Un-rendered map", "There is no lightmap detected, exporting will be done without lightmaps", new ImColor() { Value = new Num.Vector4(0.75f, 0f, 0f, 1f) }) {
                                    Buttons = new [] {
                                        new ConfirmPopup.Button("Export anyways", () => RMeshProvider.SaveToFile(path, document.Map, null, null, false)),
                                        new ConfirmPopup.Button("Don't export", () => { }),
                                    }.ToImmutableArray(),
                                });
                            // }
                        }
                        else {
                            RMeshProvider.SaveToFile(path, document.Map, document.MGLightmaps.ToArray(), LegacyLightmapper.lastBakeFaces.Select(x => x.OriginalFace).ToArray(), LegacyLightmapper.lastBakeLightmapFaces);
                        }
                    }
                }
            }
            ImGui.Separator();
            ImGui.PopItemWidth();
        }
    }
}

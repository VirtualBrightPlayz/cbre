using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
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
        private int shadowTextureDims;
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

        private Lightmapper lightmapper;

        public ExportPopup(Document document) : base("Export / Compile") {
            this.document = document;
            shadowTextureDims = LightmapConfig.ShadowTextureDims;
            textureDims = LightmapConfig.TextureDims;
            downscaleFactor = LightmapConfig.DownscaleFactor;
            ambientLightColor = Color.FromArgb(
                (byte)LightmapConfig.AmbientColorR,
                (byte)LightmapConfig.AmbientColorG,
                (byte)LightmapConfig.AmbientColorB);
            blurRadius = LightmapConfig.BlurRadius;
            planeMargin = LightmapConfig.PlaneMargin;
            bakeModels = LightmapConfig.BakeModels;
            bakeModelLightmaps = LightmapConfig.BakeModelLightmaps;
            computeShadows = LightmapConfig.ComputeShadows;
            bakeGamma = LightmapConfig.BakeGamma;
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            ImGui.SetWindowSize(new Num.Vector2(300,200), ImGuiCond.FirstUseEver);
            shouldBeOpen = true;

            ImGui.PushItemWidth(100.0f);
            
            ImGui.Text("Lightmap dimensions");
            ImGui.SameLine();
            ImGui.InputInt("##lmDim", ref textureDims);
            
            ImGui.Text("Shadowmap dimensions");
            ImGui.SameLine();
            ImGui.InputInt("##smDim", ref shadowTextureDims);
            
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
            ambientLightColor = Color.FromArgb((byte)(ambientLight.X * 255), (byte)(ambientLight.Y * 255), (byte)(ambientLight.Z * 255));

            ImGui.Text("Ambient light normal");
            Num.Vector3 ambientNormal = new(ambientLightNormal.X, ambientLightNormal.Y, ambientLightNormal.Z);
            ImGui.InputFloat3("##ambientLightNormal", ref ambientNormal);
            ambientLightNormal = ambientNormal;

            if (ImGui.Button("Apply render settings")) {
                LightmapConfig.DownscaleFactor = downscaleFactor;
                LightmapConfig.BlurRadius = blurRadius;
                LightmapConfig.ShadowTextureDims = shadowTextureDims;
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
            }
            
            ImGui.PopItemWidth();
            
            ImGui.PushItemWidth(200.0f);

            if (ImGui.Button("Clear baked data")) {
                if (document.MGLightmaps is not null) {
                    foreach (var lm in document.MGLightmaps) {
                        lm.Dispose();
                    }
                    document.MGLightmaps = null;
                }
                foreach (var face in document.BakedFaces) {
                    document.ObjectRenderer.RemoveFace(face);
                }
                document.BakedFaces.Clear();
                LegacyLightmapper.lastBakeFaces = null;
            }
            ImGui.Separator();
            ImGui.Text("Modern Lightmapper (Supports limited features)");
            if (ImGui.Button("Render##new")) {
                Task.Run(async () => {
                    try {
                        lightmapper = new Lightmapper(document);
                        await lightmapper.RenderShadowMapped(false);
                    } catch (Exception e) {
                        Mediator.Publish(EditorMediator.CompileFailed, document);
                        Logging.Logger.ShowException(e);
                    }
                });
            }
            if (ImGui.Button("Render (Debug)##newdbg")) {
                Task.Run(async () => {
                    try {
                        lightmapper = new Lightmapper(document);
                        await lightmapper.RenderShadowMapped(true);
                    } catch (Exception e) {
                        Mediator.Publish(EditorMediator.CompileFailed, document);
                        Logging.Logger.ShowException(e);
                    }
                });
            }

            if (ImGui.Button("Export .rmesh##new")) {
                var result = NativeFileDialog.SaveDialog.Open("rmesh", Directory.GetCurrentDirectory(), out string path);
                if (result == NativeFileDialog.Result.Okay) {
                    if (document.MGLightmaps == null || document.MGLightmaps.Count == 0 || document.BakedFaces == null) {
                        GameMain.Instance.Popups.Add(new ConfirmPopup("Un-rendered map", "There is no lightmap detected, exporting will be done without lightmaps", new ImColor() { Value = new Num.Vector4(0.75f, 0f, 0f, 1f) }) {
                            Buttons = new [] {
                                new ConfirmPopup.Button("Export anyways", () => RMeshProvider.SaveToFile(path, document.Map, null, null, false)),
                                new ConfirmPopup.Button("Don't export", () => { }),
                            }.ToImmutableArray(),
                        });
                    }
                    else {
                        RMeshProvider.SaveToFile(path, document.Map, document.MGLightmaps.ToArray(), document.BakedFaces.ToArray(), true);
                    }
                }
            }

            ImGui.Separator();
            ImGui.Text("Legacy Lightmapper (Supports all features)");
            if (LegacyLightmapper.FaceRenderThreads == null || LegacyLightmapper.FaceRenderThreads.Count == 0) {
                if (ImGui.Button("Render##legacy")) {
                    Task.Run(() => {
                        try {
                            LegacyLightmapper.Render(document, out _, out _, LightmapConfig.BakeModels);
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
                        if (document.MGLightmaps == null || document.MGLightmaps.Count == 0 || LegacyLightmapper.lastBakeFaces == null) {
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
            ImGui.PopItemWidth();
        }
    }
}

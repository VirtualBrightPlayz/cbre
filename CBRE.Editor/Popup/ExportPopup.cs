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
        public bool computeShadows;

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
            computeShadows = LightmapConfig.ComputeShadows;
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

            ImGui.Text("Compute shadows");
            ImGui.SameLine();
            ImGui.Checkbox("##computeShadows", ref computeShadows);
            
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
            if (LegacyLightmapper.FaceRenderThreads == null || LegacyLightmapper.FaceRenderThreads.Count == 0) {
                if (ImGui.Button("Render")) {
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
                    LightmapConfig.ComputeShadows = computeShadows;
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

                if (ImGui.Button("Export .rmesh")) {
                    var result = NativeFileDialog.SaveDialog.Open("rmesh", Directory.GetCurrentDirectory(), out string path);
                    if (result == NativeFileDialog.Result.Okay) {
                        var visibleMeshes = new List<RMesh.RMesh.VisibleMesh>();
                        var invisibleCollisionMeshes = new List<RMesh.RMesh.InvisibleCollisionMesh>();

                        var vertices = new List<RMesh.RMesh.VisibleMesh.Vertex>();
                        var triangles = new List<RMesh.RMesh.Triangle>();
                        int indexOffset = 0;
                        foreach (var solid in document.Map.WorldSpawn.GetSelfAndAllChildren().OfType<Solid>()) {
                            foreach (var face in solid.Faces) {
                                vertices.Clear();
                                triangles.Clear();
                                indexOffset = 0;

                                if (face.Texture?.Texture == null) continue;
                                // if (face.Texture.Name.ToLowerInvariant() == "tooltextures/invisible_collision") continue;
                                // if (face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent)) continue;
                                if (face.Texture.Name.ToLowerInvariant() == "tooltextures/remove_face") continue;
                                if (face.Texture.Name.ToLowerInvariant() == "tooltextures/block_light") continue;

                                face.CalculateTextureCoordinates(true);

                                vertices.AddRange(face.Vertices.Select(fv => new RMesh.RMesh.VisibleMesh.Vertex(
                                    new Vector3F(fv.Location.XZY()),
                                    new Vector2F((float)fv.TextureU, (float)fv.TextureV),
                                    face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? Vector2F.Zero : new Vector2F((float)fv.LMU, (float)fv.LMV), face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? System.Drawing.Color.Transparent : System.Drawing.Color.White)));
                                triangles.AddRange(face.GetTriangleIndices().Chunk(3).Select(c => new RMesh.RMesh.Triangle(
                                    (ushort)(c[0] + indexOffset), (ushort)(c[1] + indexOffset), (ushort)(c[2] + indexOffset))));
                                indexOffset += face.Vertices.Count;

                                if (face.Texture.Name.ToLowerInvariant() == "tooltextures/invisible_collision") {
                                    var mesh = new RMesh.RMesh.InvisibleCollisionMesh(vertices.ToImmutableArray().Select(x => new RMesh.RMesh.InvisibleCollisionMesh.Vertex(x.Position)).ToImmutableArray(), triangles.ToImmutableArray());
                                    invisibleCollisionMeshes.Add(mesh);
                                }
                                else {
                                    // var diff = System.IO.Path.GetFileName((face.Texture.Texture as AsyncTexture).Filename);
                                    var diff = face.Texture.Name+System.IO.Path.GetExtension((face.Texture.Texture as AsyncTexture).Filename);
                                    // diff = string.Empty;
                                    var lm = System.IO.Path.GetFileName(path)+"_lm.png";
                                    var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), diff, lm, face.Texture.Texture.Flags.HasFlag(Common.TextureFlags.Transparent) ? RMesh.RMesh.VisibleMesh.BlendMode.Translucent : RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped);
                                    visibleMeshes.Add(mesh);
                                }
                            }
                        }
                        
                        // LegacyLightmapper.SaveLightmaps(document, 1, path, false);
                        var texture = document.MGLightmaps[0];
                        FileStream fs = File.OpenWrite(path+"_lm.png");
                        texture.SaveAsPng(fs, texture.Width, texture.Height);
                        fs.Close();
                        // var mesh = new RMesh.RMesh.VisibleMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray(), "", DocumentManager.CurrentDocument.MapFileName+"_lm0.png", RMesh.RMesh.VisibleMesh.BlendMode.Lightmapped);
                        // visibleMeshes.Add(mesh);
                        
                        RMesh.RMesh rmesh = new RMesh.RMesh(
                            visibleMeshes.ToImmutableArray(),
                            invisibleCollisionMeshes.ToImmutableArray(),
                            null, null);

                        RMesh.RMesh.Saver.ToFile(rmesh, path);
                    }
                }
            }
            ImGui.PopItemWidth();
        }
    }
}

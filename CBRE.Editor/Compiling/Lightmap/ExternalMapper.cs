#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CBRE.Common;
using CBRE.Common.Mediator;
using CBRE.Graphics;
using CBRE.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Schema2;

namespace CBRE.Editor.Compiling.Lightmap;

sealed partial class Lightmapper {
    private record ExternalLightData(float size);

    public async Task RenderExternal(bool debug = false) {
        CancellationToken token = new CancellationToken();

        await WaitForRender("External Init", null, token);

        // var pointLights = ExtractPointLights();
        var pointLights = Document.Map.WorldSpawn.Find(e => string.Equals(e.GetEntityData()?.Name, "light", StringComparison.OrdinalIgnoreCase))
            .ToImmutableArray();
        // var spotLights = ExtractSpotLights();

        ImmutableArray<Atlas> atlases = new ImmutableArray<Atlas>();
        var gd = GlobalGraphics.GraphicsDevice;

        atlases = PrepareAtlases();

        await WaitForRender("External UV coords", () => {
            if (Document.MGLightmaps is not null) {
                foreach (var lm in Document.MGLightmaps) {
                    lm.Dispose();
                }
                Document.MGLightmaps = null;
            }
            foreach (var face in Document.BakedFaces) {
                Document.ObjectRenderer.RemoveFace(face);
            }
            Document.BakedFaces.Clear();
            Document.MGLightmaps ??= new List<Texture2D>();
        }, token);

        UpdateProgress("Calculating brightness levels... (Step 3/3)", 0);
        float scale = 1f / 128f;
        // int progressCount = 0;
        // int progressMax = atlases.Length * (pointLights.Length + spotLights.Length);
            ModelRoot root = ModelRoot.CreateModel();
            for (int atlasIndex = 0; atlasIndex < atlases.Length; atlasIndex++) {
                var atlas = atlases[atlasIndex];
                Node n = root.CreateLogicalNode();
                var meshBuilder = new MeshBuilder<VertexPositionNormal, SharpGLTF.Geometry.VertexTypes.VertexTexture2>($"atlas_{atlasIndex}");
                var prim = meshBuilder.UsePrimitive(new SharpGLTF.Materials.MaterialBuilder("Default"));
                foreach (var group in atlas.Groups) {
                    foreach (var face in group.Faces) {
                        foreach (var tri in face.GetTriangles()) {
                            var p0 = new VertexPositionNormal(tri[0].Location.ToNum() * scale, face.Normal.ToNum());
                            var p1 = new VertexPositionNormal(tri[1].Location.ToNum() * scale, face.Normal.ToNum());
                            var p2 = new VertexPositionNormal(tri[2].Location.ToNum() * scale, face.Normal.ToNum());
                            var t0 = new VertexTexture2(new System.Numerics.Vector2(tri[0].DiffU, tri[0].DiffV), new System.Numerics.Vector2(tri[0].LMU, tri[0].LMV));
                            var t1 = new VertexTexture2(new System.Numerics.Vector2(tri[1].DiffU, tri[1].DiffV), new System.Numerics.Vector2(tri[1].LMU, tri[1].LMV));
                            var t2 = new VertexTexture2(new System.Numerics.Vector2(tri[2].DiffU, tri[2].DiffV), new System.Numerics.Vector2(tri[2].LMU, tri[2].LMV));
                            var v0 = new VertexBuilder<VertexPositionNormal, VertexTexture2, VertexEmpty>(p0, t0);
                            var v1 = new VertexBuilder<VertexPositionNormal, VertexTexture2, VertexEmpty>(p1, t1);
                            var v2 = new VertexBuilder<VertexPositionNormal, VertexTexture2, VertexEmpty>(p2, t2);
                            var val = prim.AddTriangle(v2, v1, v0);
                            /*if (val.A == -1 || val.B == -1 || val.C == -1) {
                                Debugger.Break();
                            }*/
                        }
                    }
                }
                var mesh = root.CreateMesh(meshBuilder);
                n.Mesh = mesh;
            }

            for (int i = 0; i < pointLights.Length; i++) {
                Node n = root.CreateLogicalNode();
                var light = root.CreatePunctualLight($"pointlight_{i}", PunctualLightType.Point);
                var data = pointLights[i].GetEntityData();
                float getPropertyFloat(string key)
                    => float.TryParse(data.GetPropertyValue(key), NumberStyles.Any, CultureInfo.InvariantCulture,
                        out float v)
                        ? v
                        : 0.0f;
                light.Color = data.GetPropertyVector3("color").ToNum() / 255f;
                light.Range = getPropertyFloat("range") * scale;
                light.Intensity = getPropertyFloat("intensity");
                light.Extras = SharpGLTF.IO.JsonContent.Serialize(new ExternalLightData(MathF.Max(1f, getPropertyFloat("size")) * scale));
                n.PunctualLight = light;
                n.WithLocalTranslation(pointLights[i].BoundingBox.Center.ToNum() * scale);
            }

            {
                // var cam = Document.Map.GetActiveCamera();
                // Node n = root.CreateLogicalNode();
                // var cam2 = root.CreateCamera("ActiveCamera");
                // cam2.SetPerspectiveMode(1f, 90 * MathF.PI / 180f, 0.01f, 1000f);
                // n.Camera = cam2;
                // n.WithLocalTranslation(cam.EyePosition.ToNum() * scale);
                // n.WithLocalRotation(System.Numerics.Quaternion.CreateFromRotationMatrix(System.Numerics.Matrix4x4.CreateLookAt(cam.EyePosition.ToNum() * scale, cam.LookPosition.ToNum(), cam.GetUp().ToNum())));
            }

            string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"input.glb");
            root.SaveGLB(fname);

            for (int atlasIndex = 0; atlasIndex < atlases.Length; atlasIndex++) {
                string fname2 = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"output_{atlasIndex}.png");
                string cpath = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..");
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Directory.GetFiles(cpath).FirstOrDefault(x => Path.GetFileName(x).ToLower().StartsWith("raytrace-lm"));
                // info.FileName = "raytrace-lm";
                info.ArgumentList.Add("-in");
                info.ArgumentList.Add(fname);
                info.ArgumentList.Add("-out");
                info.ArgumentList.Add(fname2);
                info.ArgumentList.Add("-render");
                info.ArgumentList.Add($"atlas_{atlasIndex}");
                info.WorkingDirectory = cpath;
                info.UseShellExecute = true;

                using var proc = Process.Start(info);
                await proc.WaitForExitAsync(token);

                if (proc.ExitCode != 0) {
                    throw new Exception("Error in external lightmapping program!");
                }

                using var fileData = File.OpenRead(fname2);
                Texture2D tex = Texture2D.FromStream(GlobalGraphics.GraphicsDevice, fileData);

                await WaitForRender("Add Texture External", () => {
                    Document.MGLightmaps.Add(tex);
                }, token);
            }

        UpdateProgress("Lightmapping complete!", 1.0f);
        await WaitForRender("Cleanup", () => {
            foreach (var face in ModelFaces) {
                Document.BakedFaces.Add(face.OriginalFace);
                Document.ObjectRenderer.AddFace(face.OriginalFace);
            }
            Document.ObjectRenderer.MarkDirty();
            foreach (var atlas in atlases) {
                atlas?.Dispose();
            }
        }, token);
    }

}

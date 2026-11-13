using CBRE.DataStructures.Geometric;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.UI;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.UI;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace CBRE.Editor.Compiling.Lightmap
{
    public class ExternalMapper
    {
        public static System.Numerics.Vector3 CbreToSystem(DataStructures.Geometric.CoordinateF coordinate)
        {
            return new System.Numerics.Vector3(coordinate.X, coordinate.Y, coordinate.Z);
        }

        public static byte[] BitmapToBytes(Bitmap bitmap)
        {
            byte[] data = new byte[bitmap.Width * bitmap.Height * 4];
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    int offset = (y * bitmap.Width + x) * 4;
                    data[offset + 0] = color.R;
                    data[offset + 1] = color.G;
                    data[offset + 2] = color.B;
                    data[offset + 3] = color.A;
                }
            }
            return data;
        }

        public static float[] BitmapToFloats(Bitmap bitmap)
        {
            float[] data = new float[bitmap.Width * bitmap.Height * 4];
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    int offset = (y * bitmap.Width + x) * 4;
                    data[offset + 0] = color.R / 255f;
                    data[offset + 1] = color.G / 255f;
                    data[offset + 2] = color.B / 255f;
                    data[offset + 3] = color.A / 255f;
                }
            }
            return data;
        }

        public static void BytesToBitmap(Bitmap bitmap, byte[] data)
        {
            if (data.Length < bitmap.Width * bitmap.Height * 4)
            {
                throw new Exception("This should not happen, but data is too small to be copied to the bitmap.");
            }
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    int offset = (y * bitmap.Width + x) * 4;
                    Color color = Color.FromArgb(data[offset + 3], data[offset + 0], data[offset + 1], data[offset + 2]);
                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        public static void FloatsToBitmap(Bitmap bitmap, float[] data)
        {
            if (data.Length < bitmap.Width * bitmap.Height * 4)
            {
                throw new Exception("This should not happen, but data is too small to be copied to the bitmap.");
            }
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    int offset = (y * bitmap.Width + x) * 4;
                    byte r = (byte)Math.Max(Math.Min(data[offset + 0] * 255.0f, 255.0f), 0.0f);
                    byte g = (byte)Math.Max(Math.Min(data[offset + 1] * 255.0f, 255.0f), 0.0f);
                    byte b = (byte)Math.Max(Math.Min(data[offset + 2] * 255.0f, 255.0f), 0.0f);
                    byte a = (byte)Math.Max(Math.Min(data[offset + 3] * 255.0f, 255.0f), 0.0f);
                    Color color = Color.FromArgb(a, r, g, b);
                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        public static void ExportGLB(string filename, Document document, ExportForm exportForm, out List<LightmapGroup> lmGroups)
        {
            Lightmapper.CollectMapData(document, exportForm, out List<LMFace> faces, out lmGroups, out List<Light> lightEntities, out int lmCount, out List<LMFace> allBlockers, out int blockerCount, out int modelBlockerCount);
            if (lmCount != 1)
            {
                throw new Exception("Could not fit lightmap into one texture; try increasing texture dimensions or downscale factor; External Lightmapping has a maximum of 1 lightmap texture!");
            }
            foreach (LightmapGroup group in lmGroups)
            {
                foreach (LMFace face in group.Faces)
                {
                    Lightmapper.UpdateLightmapUVs(document, group, face, LightmapConfig.TextureDims);
                }
            }
            Lightmapper.UpdateProgress(exportForm, "Exporting glb file...", 0.05f);

            ITextureStreamSource streamSource = document.TextureCollection.GetStreamSource(0, 0);
            using MemoryStream memStream = new MemoryStream();

            float scale = 1f / 160f;
            ModelRoot root = ModelRoot.CreateModel();
            Scene scene = root.UseScene(0);
            {
                scene.Extras = new System.Text.Json.Nodes.JsonObject();
                scene.Extras["ambient_light_color_red"] = LightmapConfig.AmbientColorR / 255f;
                scene.Extras["ambient_light_color_green"] = LightmapConfig.AmbientColorG / 255f;
                scene.Extras["ambient_light_color_blue"] = LightmapConfig.AmbientColorB / 255f;
                Dictionary<string, PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, VertexPositionNormal, VertexTexture2, VertexEmpty>> dict = new Dictionary<string, PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, VertexPositionNormal, VertexTexture2, VertexEmpty>>();
                Node n = scene.CreateNode();
                MeshBuilder<VertexPositionNormal, VertexTexture2> meshBuilder = new MeshBuilder<VertexPositionNormal, VertexTexture2>("mesh");
                {
                    foreach (LMFace face in allBlockers)
                    {
                        PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, VertexPositionNormal, VertexTexture2, VertexEmpty> prim = null;
                        string name = face.OriginalFace.Texture.Name;
                        if (dict.ContainsKey(name))
                        {
                            prim = dict[name];
                        }
                        else
                        {
                            TextureItem item = document.TextureCollection.GetItem(name);
                            if (item != null && streamSource.HasImage(item))
                            {
                                BitmapRef bitmapRef = streamSource.GetImage(item);
                                memStream.Position = 0;
                                bitmapRef.Bitmap.Save(memStream, ImageFormat.Png);
                                SharpGLTF.Materials.MaterialBuilder materialBuilder = new SharpGLTF.Materials.MaterialBuilder(name).WithChannelImage(SharpGLTF.Materials.KnownChannel.BaseColor, memStream.ToArray());
                                prim = meshBuilder.UsePrimitive(materialBuilder);
                            }
                            else
                            {
                                prim = meshBuilder.UsePrimitive(new SharpGLTF.Materials.MaterialBuilder(name));
                            }
                            dict.Add(name, prim);
                        }
                        foreach (LMFace.Vertex[] tri in face.GetTriangles())
                        {
                            float uvScale = face.LmIndex < 0 ? 0f : 1f;
                            var p0 = new VertexPositionNormal(tri[0].Location.X * scale, tri[0].Location.Y * scale, tri[0].Location.Z * scale, face.Normal.X, face.Normal.Y, face.Normal.Z);
                            var p1 = new VertexPositionNormal(tri[1].Location.X * scale, tri[1].Location.Y * scale, tri[1].Location.Z * scale, face.Normal.X, face.Normal.Y, face.Normal.Z);
                            var p2 = new VertexPositionNormal(tri[2].Location.X * scale, tri[2].Location.Y * scale, tri[2].Location.Z * scale, face.Normal.X, face.Normal.Y, face.Normal.Z);
                            var t0 = new VertexTexture2(new System.Numerics.Vector2(tri[0].DiffU, tri[0].DiffV), new System.Numerics.Vector2(tri[0].LMU, tri[0].LMV) * uvScale);
                            var t1 = new VertexTexture2(new System.Numerics.Vector2(tri[1].DiffU, tri[1].DiffV), new System.Numerics.Vector2(tri[1].LMU, tri[1].LMV) * uvScale);
                            var t2 = new VertexTexture2(new System.Numerics.Vector2(tri[2].DiffU, tri[2].DiffV), new System.Numerics.Vector2(tri[2].LMU, tri[2].LMV) * uvScale);
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
                Mesh mesh = root.CreateMesh(meshBuilder);
                n.Mesh = mesh;
            }

            for (int i = 0; i < lightEntities.Count; i++)
            {
                Light data = lightEntities[i];
                if (data.Direction != null && data.innerCos != null && data.outerCos != null)
                {
                    Node n = scene.CreateNode();
                    PunctualLight light = root.CreatePunctualLight($"spotlight_{i}", PunctualLightType.Spot);
                    light.Color = CbreToSystem(data.Color) / 255f;
                    light.Range = data.Range * scale;
                    light.Intensity = data.Intensity;
                    light.SetSpotCone((float)Math.Acos(data.innerCos.Value), (float)Math.Acos(data.outerCos.Value));
                    //light.Extras = SharpGLTF.IO.JsonContent.Serialize(new ExternalLightData(MathF.Max(1f, getPropertyFloat("size")) * scale));
                    n.PunctualLight = light;
                    n.WithLocalTranslation(CbreToSystem(data.Origin) * scale);
                    n.WithLocalRotation(System.Numerics.Quaternion.CreateFromAxisAngle(CbreToSystem(data.Direction), 0f));
                }
                else
                {
                    Node n = scene.CreateNode();
                    PunctualLight light = root.CreatePunctualLight($"pointlight_{i}", PunctualLightType.Point);
                    light.Color = CbreToSystem(data.Color) / 255f;
                    light.Range = data.Range * scale;
                    light.Intensity = data.Intensity;
                    //light.Extras = SharpGLTF.IO.JsonContent.Serialize(new ExternalLightData(MathF.Max(1f, getPropertyFloat("size")) * scale));
                    n.PunctualLight = light;
                    n.WithLocalTranslation(CbreToSystem(data.Origin) * scale);
                }
            }

            root.SaveGLB(filename);
            Lightmapper.UpdateProgress(exportForm, "Finished exporting glb file.", 0.1f);
        }

        public static void RenderExternal(Document document, ExportForm exportForm)
        {
            string wkDir = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..");
            string tempGlb = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bake.glb");

            ExportGLB(tempGlb, document, exportForm, out List<LightmapGroup> lmGroups);

            string executableFile = string.Empty;
            string processArgs = string.Empty;
            foreach (string path in Directory.GetFiles(wkDir))
            {
                string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
                string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                // Maybe one day CBRE-EX will have support for running on Wine/Linux.
                // Also .sh files *can* work on Windows if you have software installed for it.
                if (name == "bake")
                {
                    switch (extension)
                    {
                        case ".exe":
                            executableFile = path;
                            processArgs = string.Empty;
                            break;
                        case ".bat":
                            executableFile = "cmd.exe";
                            processArgs = $"/c {path}";
                            break;
                        case ".ps1":
                            executableFile = "powershell.exe";
                            processArgs = $"-File {path}";
                            break;
                        case ".sh":
                            executableFile = "bash";
                            processArgs = path;
                            break;
                        case ".py":
                            executableFile = "python";
                            processArgs = path;
                            break;
                    }
                    break;
                }
            }

            if (string.IsNullOrEmpty(executableFile))
            {
                throw new Exception("No bake script found!");
            }

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = executableFile;
            info.Arguments = $"{processArgs} -file {tempGlb.Replace('\\', '/')} -size {LightmapConfig.TextureDims}";
            info.WorkingDirectory = wkDir;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            Lightmapper.UpdateProgress(exportForm, "Starting external lightmapping process...", 0.15f);
            Process proc = Process.Start(info);
            proc.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    return;
                Lightmapper.UpdateProgress(exportForm, e.Data, 0.5f);
            };
            proc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    return;
                Lightmapper.UpdateProgress(exportForm, e.Data, 0.5f);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new Exception("Error in external lightmapping program!");
            }

            string colorFile = System.IO.Path.Combine(wkDir, "color.png");

            using Bitmap bitmap = new Bitmap(System.Drawing.Image.FromFile(colorFile));
            float[] buffer = BitmapToFloats(bitmap);
            float[][] buffers = new float[4][];
            buffers[0] = new float[buffer.Length];
            Array.Copy(buffer, buffers[0], buffer.Length);
            buffers[1] = new float[buffer.Length];
            Array.Copy(buffer, buffers[1], buffer.Length);
            buffers[2] = new float[buffer.Length];
            Array.Copy(buffer, buffers[2], buffer.Length);
            buffers[3] = new float[buffer.Length];
            Array.Copy(buffer, buffers[3], buffer.Length);

            int totalTextureDims = LightmapConfig.TextureDims;
            Lightmapper.UpdateProgress(exportForm, "Blurring lightmap...", 0.95f);
            float[] blurBuffer = new float[buffers[0].Length];
            for (int k = 0; k < 4; k++)
            {
                foreach (LightmapGroup group in lmGroups)
                {
                    int downscaledWidth = (int)Math.Ceiling(group.Width / LightmapConfig.DownscaleFactor);
                    int downscaledHeight = (int)Math.Ceiling(group.Height / LightmapConfig.DownscaleFactor);

                    //CoordinateF ambientNormal = new CoordinateF(LightmapConfig.AmbientNormalX,
                    //                                            LightmapConfig.AmbientNormalY,
                    //                                            LightmapConfig.AmbientNormalZ).Normalise();
                    //float ambientMultiplier = (group.Plane.Normal.Dot(ambientNormal) + 1.5f) * 0.4f;
                    //CoordinateF mAmbientColor = new CoordinateF((LightmapConfig.AmbientColorB * ambientMultiplier / 255.0f),
                    //                                        (LightmapConfig.AmbientColorG * ambientMultiplier / 255.0f),
                    //                                        (LightmapConfig.AmbientColorR * ambientMultiplier / 255.0f));
                    for (int y = group.writeY; y < group.writeY + downscaledHeight; y++)
                    {
                        if (y < 0 || y >= totalTextureDims) continue;
                        for (int x = group.writeX; x < group.writeX + downscaledWidth; x++)
                        {
                            if (x < 0 || x >= totalTextureDims) continue;
                            int offset = (x + y * totalTextureDims) * 4;

                            float accumRed = 0;
                            float accumGreen = 0;
                            float accumBlue = 0;
                            int sampleCount = 0;
                            for (int j = -LightmapConfig.BlurRadius; j <= LightmapConfig.BlurRadius; j++)
                            {
                                if (y + j < 0 || y + j >= totalTextureDims) continue;
                                if (y + j < group.writeY || y + j >= group.writeY + downscaledHeight) continue;
                                for (int i = -LightmapConfig.BlurRadius; i <= LightmapConfig.BlurRadius; i++)
                                {
                                    if (i * i + j * j > LightmapConfig.BlurRadius * LightmapConfig.BlurRadius) continue;
                                    if (x + i < 0 || x + i >= totalTextureDims) continue;
                                    if (x + i < group.writeX || x + i >= group.writeX + downscaledWidth) continue;
                                    int sampleOffset = ((x + i) + (y + j) * totalTextureDims) * 4;
                                    if (buffers[k][sampleOffset + 3] <= 0.0f) continue;
                                    sampleCount++;
                                    accumRed += buffers[k][sampleOffset + 0];
                                    accumGreen += buffers[k][sampleOffset + 1];
                                    accumBlue += buffers[k][sampleOffset + 2];
                                }
                            }

                            if (sampleCount < 1) sampleCount = 1;
                            accumRed /= sampleCount;
                            accumGreen /= sampleCount;
                            accumBlue /= sampleCount;

                            //accumRed = mAmbientColor.X + (accumRed * (1.0f - mAmbientColor.X));
                            //accumGreen = mAmbientColor.Y + (accumGreen * (1.0f - mAmbientColor.Y));
                            //accumBlue = mAmbientColor.Z + (accumBlue * (1.0f - mAmbientColor.Z));

                            if (accumRed > 1.0f) accumRed = 1.0f;
                            if (accumGreen > 1.0f) accumGreen = 1.0f;
                            if (accumBlue > 1.0f) accumBlue = 1.0f;

                            blurBuffer[offset + 0] = accumRed;
                            blurBuffer[offset + 1] = accumGreen;
                            blurBuffer[offset + 2] = accumBlue;
                            blurBuffer[offset + 3] = 1.0f;
                        }
                    }
                }

                blurBuffer.CopyTo(buffers[k], 0);
            }

            for (int i = 0; i < buffers[0].Length; i++)
            {
                if (i % 4 == 3)
                {
                    buffers[0][i] = 1.0f;
                    buffers[1][i] = 1.0f;
                    buffers[2][i] = 1.0f;
                    buffers[3][i] = 1.0f;
                }
                else
                {
                    float brightnessAdd = (buffers[0][i] + buffers[1][i] + buffers[2][i]) / (float)Math.Sqrt(3.0);
                    if (brightnessAdd > 0.0f) //normalize brightness to remove artifacts when adding together
                    {
                        buffers[0][i] *= buffers[3][i] / brightnessAdd;
                        buffers[1][i] *= buffers[3][i] / brightnessAdd;
                        buffers[2][i] *= buffers[3][i] / brightnessAdd;
                    }
                }
            }

            Lightmapper.UpdateProgress(exportForm, "Copying bitmap data...", 0.99f);
            TextureCollection textureCollection = document.TextureCollection;
            for (int k = 0; k < 4; k++)
            {
                Bitmap bitmapFinal = new Bitmap(totalTextureDims, totalTextureDims);
                FloatsToBitmap(bitmapFinal, buffers[k]);
                lock (textureCollection.Lightmaps)
                {
                    textureCollection.Lightmaps[k] = bitmapFinal;
                }
            }

            lock (textureCollection.Lightmaps)
            {
                document.TextureCollection.LightmapTextureOutdated = true;
            }

            foreach (Viewport3D viewport in ViewportManager.Viewports.Where(vp => vp is Viewport3D).Select(vp => vp as Viewport3D))
            {
                viewport.Type = Viewport3D.ViewType.Lightmapped;
                ViewportLabelListener listener = viewport.Listeners.Find(l => l is ViewportLabelListener) as ViewportLabelListener;
                listener?.Rebuild();
            }
            GC.Collect();

            Lightmapper.UpdateProgress(exportForm, "Lightmapping complete!", 1.0f);
        }

#if false
        public async Task RenderExternal(bool debug = false)
        {
            CancellationToken token = new CancellationToken();

            await WaitForRender("External Init", null, token);

            var pointLights = ExtractPointLights();
            /*var pointLights = Document.Map.WorldSpawn.Find(e => string.Equals(e.GetEntityData()?.Name, "light", StringComparison.OrdinalIgnoreCase))
                .ToImmutableArray();*/
            var spotLights = ExtractSpotLights();

            ImmutableArray<Atlas> atlases = new ImmutableArray<Atlas>();
            var gd = GlobalGraphics.GraphicsDevice;

            atlases = PrepareAtlases();

            await WaitForRender("External UV coords", () =>
            {
                if (Document.MGLightmaps is not null)
                {
                    foreach (var lm in Document.MGLightmaps)
                    {
                        lm?.Dispose();
                    }
                    Document.MGLightmaps = null;
                }
                foreach (var face in Document.BakedFaces)
                {
                    Document.ObjectRenderer.RemoveFace(face);
                }
                Document.BakedFaces.Clear();
                Document.MGLightmaps ??= new List<Texture2D>();
            }, token);

            UpdateProgress("Calculating brightness levels... (Step 3/3)", 0);
            float scale = 1f / 128f;
            ModelRoot root = ModelRoot.CreateModel();
            for (int atlasIndex = 0; atlasIndex < atlases.Length; atlasIndex++)
            {
                Dictionary<ITexture, PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, VertexPositionNormal, VertexTexture2, VertexEmpty>> dict = new Dictionary<ITexture, PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, VertexPositionNormal, VertexTexture2, VertexEmpty>>();
                Node n = root.CreateLogicalNode();
                var meshBuilder = new MeshBuilder<VertexPositionNormal, SharpGLTF.Geometry.VertexTypes.VertexTexture2>($"atlas{atlasIndex}");
                var atlas = atlases[atlasIndex];
                foreach (var group in atlas.Groups)
                {
                    foreach (var face in group.Faces)
                    {
                        PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, VertexPositionNormal, VertexTexture2, VertexEmpty> prim = null;
                        if (dict.ContainsKey(face.Texture.Texture))
                        {
                            prim = dict[face.Texture.Texture];
                        }
                        else
                        {
                            string path = face.Texture.Texture is AsyncTexture tex ? tex.Filename : face.Texture.Name;
                            prim = meshBuilder.UsePrimitive(new SharpGLTF.Materials.MaterialBuilder(face.Texture.Name).WithChannelImage("BaseColor", path).WithMetallicRoughness(0f, 1f));
                        }
                        foreach (var tri in face.GetTriangles())
                        {
                            var normal = -(tri[2].Location - tri[0].Location).Cross(tri[2].Location - tri[1].Location);
                            var p0 = new VertexPositionNormal(tri[0].Location.ToNum() * scale, normal.ToNum());
                            var p1 = new VertexPositionNormal(tri[1].Location.ToNum() * scale, normal.ToNum());
                            var p2 = new VertexPositionNormal(tri[2].Location.ToNum() * scale, normal.ToNum());
                            var t0 = new VertexTexture2(new System.Numerics.Vector2(tri[0].DiffU, tri[0].DiffV), new System.Numerics.Vector2(tri[0].LMU, tri[0].LMV));
                            var t1 = new VertexTexture2(new System.Numerics.Vector2(tri[1].DiffU, tri[1].DiffV), new System.Numerics.Vector2(tri[1].LMU, tri[1].LMV));
                            var t2 = new VertexTexture2(new System.Numerics.Vector2(tri[2].DiffU, tri[2].DiffV), new System.Numerics.Vector2(tri[2].LMU, tri[2].LMV));
                            var v0 = new VertexBuilder<VertexPositionNormal, VertexTexture2, VertexEmpty>(p0, t0);
                            var v1 = new VertexBuilder<VertexPositionNormal, VertexTexture2, VertexEmpty>(p1, t1);
                            var v2 = new VertexBuilder<VertexPositionNormal, VertexTexture2, VertexEmpty>(p2, t2);
                            var val = prim.AddTriangle(v2, v1, v0);
                        }
                    }
                }
                var mesh = root.CreateMesh(meshBuilder);
                n.Mesh = mesh;
            }

            for (int i = 0; i < pointLights.Length; i++)
            {
                Node n = root.CreateLogicalNode();
                var light = root.CreatePunctualLight($"pointlight_{i}", PunctualLightType.Point);

                light.Color = pointLights[i].RawColor.ToNum();
                light.Range = pointLights[i].Range * scale;
                light.Intensity = pointLights[i].Intensity * LightmapConfig.BakeGamma * (LightmapConfig.MultiplyIntensityByRange ? light.Range : 1);

                /*
                var data = pointLights[i].GetEntityData();
                float getPropertyFloat(string key)
                    => float.TryParse(data.GetPropertyValue(key), NumberStyles.Any, CultureInfo.InvariantCulture,
                        out float v)
                        ? v
                        : 0.0f;
                light.Color = data.GetPropertyVector3("color").ToNum() / 255f;
                light.Range = getPropertyFloat("range") * scale;
                light.Intensity = getPropertyFloat("intensity") * LightmapConfig.BakeGamma;
                light.Extras = SharpGLTF.IO.JsonContent.Serialize(new ExternalLightData(MathF.Max(1f, getPropertyFloat("size")) * scale));
                */
                n.PunctualLight = light;
                n.WithLocalTranslation(pointLights[i].BoundingBox.Center.ToNum() * scale);
            }

            for (int i = 0; i < spotLights.Length; i++)
            {
                Node n = root.CreateLogicalNode();
                var light = root.CreatePunctualLight($"spotlight_{i}", PunctualLightType.Spot);

                light.Color = spotLights[i].RawColor.ToNum();
                light.Range = spotLights[i].Range * scale;
                light.Intensity = spotLights[i].Intensity * LightmapConfig.BakeGamma * (LightmapConfig.MultiplyIntensityByRange ? light.Range : 1);
                var ang = (MathF.PI / 180f);
                ang = 1;
                float inner = MathF.Abs(spotLights[i].InnerConeAngle * ang);
                float outer = MathF.Abs(spotLights[i].OuterConeAngle * ang);
                if (inner >= outer)
                {
                    light.SetSpotCone(outer, inner);
                }
                else
                {
                    light.SetSpotCone(inner, outer);
                }

                n.PunctualLight = light;
                n.WithLocalTranslation(spotLights[i].BoundingBox.Center.ToNum() * scale);
                var rot = Quaternion.CreateFromAxisAngle(spotLights[i].Direction, 0);
                n.WithLocalRotation(new System.Numerics.Quaternion(rot.X, rot.Y, rot.Z, rot.W));
            }

            string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"input.glb");
            root.SaveGLB(fname);

            string fname2 = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"output_{0}.txt");
            string cpath = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..");
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Directory.GetFiles(cpath).FirstOrDefault(x => Path.GetFileName(x).ToLower().StartsWith("raytrace-lm"));
            info.ArgumentList.Add(fname);
            info.ArgumentList.Add(fname2);
            info.ArgumentList.Add(LightmapConfig.TextureDims.ToString());
            info.WorkingDirectory = cpath;
            info.UseShellExecute = true;

            using var proc = Process.Start(info);
            await proc.WaitForExitAsync(token);

            if (proc.ExitCode != 0)
            {
                throw new Exception("Error in external lightmapping program!");
            }

            string lmdir = await File.ReadAllTextAsync(fname2);
            string[] files = Directory.GetFiles(lmdir, "*.png");

            for (int atlasIndex = 0; atlasIndex < atlases.Length; atlasIndex++)
            {
                string dir = Path.Combine(lmdir, $"atlas{atlasIndex}");
                dir = files.OrderByDescending(x => File.GetLastWriteTimeUtc(x).Ticks).FirstOrDefault(x => x.Contains(dir)) ?? string.Empty;
                if (!string.IsNullOrEmpty(dir) && File.Exists(dir))
                {
                    using var fileData = File.OpenRead(dir);
                    Texture2D tex = Texture2D.FromStream(GlobalGraphics.GraphicsDevice, fileData);

                    await WaitForRender("Add Texture External", () =>
                    {
                        Document.MGLightmaps.Add(tex);
                    }, token);
                }
                else
                {
                    await WaitForRender("Add Texture External", () =>
                    {
                        Document.MGLightmaps.Add(null);
                    }, token);
                }
                UpdateProgress("Calculating brightness levels... (Step 3/3)", (float)(atlasIndex + 1) / atlases.Length);
            }

            UpdateProgress("Lightmapping complete!", 1.0f);
            await WaitForRender("Cleanup", () =>
            {
                foreach (var face in ModelFaces)
                {
                    Document.BakedFaces.Add(face.OriginalFace);
                    Document.ObjectRenderer.AddFace(face.OriginalFace);
                }
                Document.ObjectRenderer.MarkDirty();
                foreach (var atlas in atlases)
                {
                    atlas?.Dispose();
                }
            }, token);
        }
#endif // false
    }
}
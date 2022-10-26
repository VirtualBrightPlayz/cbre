using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CBRE.DataStructures.Geometric;
using CBRE.Graphics;
using CBRE.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#nullable enable
namespace CBRE.Editor.Compiling.Lightmap; 

sealed partial class Lightmapper {
    private readonly struct RenderBuffer : IEnumerable<RenderBuffer.Color> {
        public struct Color {
            public float R;
            public float G;
            public float B;

            public static Color operator +(in Color a, in Color b)
                => new Color { R = a.R + b.R, G = a.G + b.G, B = a.B + b.B };

            public byte[] ToRgba32Bytes() {
                float excess = MathHelper.Clamp(
                    value: new[] { R, G, B }.Max() - 1.0f,
                    min: 0.0f, max: 1.0f);

                byte transform(float v)
                    => (byte)Math.Max(0.0f, Math.Min(byte.MaxValue, MathHelper.Lerp(v, 1.0f, excess) * 255));
                
                return new [] {
                    transform(R),
                    transform(G),
                    transform(B),
                    byte.MaxValue
                };
            }
        }
        
        private readonly Color[] colors; //TODO: use memory-mapped files

        public RenderBuffer() {
            colors = new Color[LightmapConfig.TextureDims * LightmapConfig.TextureDims];
        }

        public ref Color this[int x, int y] {
            get => ref colors[x * LightmapConfig.TextureDims + y];
        }

        public ref Color this[LightmapGroup.UvPairInt uv] {
            get => ref this[uv.U, uv.V];
        }

        public IEnumerator<Color> GetEnumerator() => ((IEnumerable<Color>)colors).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public int rayTestProgress = 0;
    public int rayTestMax = 0;
    
    public async Task RenderRayTest() {
        CancellationToken token = new CancellationToken();

        await WaitForRender("RayTest Init", null, token);

        void saveTexture(string filePath, Texture2D texture) {
            if (texture.Format != SurfaceFormat.Vector4) {
                string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", filePath);
                using var fileSaveStream = File.Open(fname, FileMode.Create);
                texture.SaveAsPng(fileSaveStream, texture.Width, texture.Height);
            } else {
                using RenderTarget2D rt = new RenderTarget2D(GlobalGraphics.GraphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                GlobalGraphics.GraphicsDevice.SetRenderTarget(rt);
                using var effect = new BasicEffect(GlobalGraphics.GraphicsDevice);
                effect.TextureEnabled = true;
                effect.Texture = texture;
                effect.CurrentTechnique.Passes[0].Apply();
                PrimitiveDrawing.Begin(PrimitiveType.QuadList);
                PrimitiveDrawing.Vertex2(-1f, -1f, 0f, 1f);
                PrimitiveDrawing.Vertex2(1f, -1f, 1f, 1f);
                PrimitiveDrawing.Vertex2(1f, 1f, 1f, 0f);
                PrimitiveDrawing.Vertex2(-1f, 1f, 0f, 0f);
                PrimitiveDrawing.End();
                GlobalGraphics.GraphicsDevice.SetRenderTarget(null);
                string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", filePath);
                using var fileSaveStream = File.Open(fname, FileMode.Create);
                rt.SaveAsPng(fileSaveStream, rt.Width, rt.Height);
            }
        }

        async Task saveTextureAsync(string filePath, Texture2D texture) {
            await WaitForRender("RayTest save texture", () => {
                saveTexture(filePath, texture);
            }, token);
        }

        var pointLights = ExtractPointLights();
        var spotLights = ExtractSpotLights();
        var atlases = PrepareAtlases();

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

        var groupToAtlas = new Dictionary<LightmapGroup, Atlas>();
        foreach (var atlas in atlases) {
            foreach (var group in atlas.Groups) {
                groupToAtlas.Add(group, atlas);
            }
        }
        Document.MGLightmaps ??= new List<Texture2D>();

        var atlasBuffers = new Dictionary<Atlas, RenderBuffer>();
        foreach (var atlas in atlases) {
            atlasBuffers.Add(atlas, new RenderBuffer());
        }

        UpdateProgress("Calculating brightness levels... (Step 3/3)", 0);
        rayTestMax = pointLights.Length + spotLights.Length;

        foreach (var light in pointLights) {
            RenderLight(light, groupToAtlas, atlasBuffers);
            rayTestProgress++;
        }

        foreach (var light in spotLights) {
            RenderLight(new PointLight(light.Location, light.Range, light.Color), groupToAtlas, atlasBuffers);
            rayTestProgress++;
        }

        for (int i=0;i<atlases.Length;i++) {
            var atlas = atlases[i];
            Texture2D texture = new Texture2D(
                GlobalGraphics.GraphicsDevice,
                LightmapConfig.TextureDims,
                LightmapConfig.TextureDims,
                mipmap: false,
                SurfaceFormat.Color);
            byte[] buffer = atlasBuffers[atlas].SelectMany(p => p.ToRgba32Bytes()).ToArray();
            byte[] byteBuffer = new byte[buffer.Length];
            for (int y = 0; y < LightmapConfig.TextureDims; y++) {
                for (int x = 0; x < LightmapConfig.TextureDims; x++) {
                    int offset = (x + y * LightmapConfig.TextureDims) * 4;
                    int offset2 = (y + x * LightmapConfig.TextureDims) * 4;
                    byteBuffer[offset+0] = buffer[offset2+0];
                    byteBuffer[offset+1] = buffer[offset2+1];
                    byteBuffer[offset+2] = buffer[offset2+2];
                    byteBuffer[offset+3] = buffer[offset2+3];
                }
            }
            texture.SetData(byteBuffer);
            string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"lm_{i}.png");
            texture.Name = $"lm_{i}";
            Document.MGLightmaps.Add(texture);
            await saveTextureAsync($"lm_{i}.png", texture);
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

        // return Task.CompletedTask;
    }

    private IEnumerable<LightmapGroup.UvPairInt> Raster(
        LightmapGroup.UvPairInt start,
        LightmapGroup.UvPairInt end)
        => Enumerable.Range(start.V, end.V - start.V + 1)
            .SelectMany(v => Enumerable.Range(start.U, end.U - start.U + 1)
                .Select(u => new LightmapGroup.UvPairInt { U = u, V = v }));
    
    private void RenderLight(
            PointLight light,
            IReadOnlyDictionary<LightmapGroup, Atlas> groupToAtlas,
            IReadOnlyDictionary<Atlas, RenderBuffer> atlasBuffers) {
        var groupsToRender = DetermineGroupsDirectlyAffectedByLightSource(light);
        int rendered = 0;

        foreach (var group in groupsToRender) {
            float progr = (float)rayTestProgress / rayTestMax;
            progr += (float)rendered / groupsToRender.Length / rayTestMax;
            UpdateProgress("Calculating brightness levels... (Step 3/3)", progr);
            var startUv = group.StartWriteUV;
            var endUv = group.EndWriteUV;

            Vector3F getWorldPosFromUv(LightmapGroup.UvPairInt uv) {
                float uAmount = (float)(uv.U - startUv.U) / (endUv.U - startUv.U);
                float vAmount = (float)(uv.V - startUv.V) / (endUv.V - startUv.V);

                return Vector3F.Lerp(
                    Vector3F.Lerp(
                        group.TopLeftWorldPos, group.BottomLeftWorldPos, vAmount),
                    Vector3F.Lerp(
                        group.TopRightWorldPos, group.BottomRightWorldPos, vAmount),
                    uAmount);
            }
            
            foreach (var uv in Raster(startUv, endUv)) {
                Vector3F world = getWorldPosFromUv(uv);
                // Line line = new Line(new DataStructures.Geometric.Vector3(world), light.Location.ToCbre());
                // if (Document.Map.WorldSpawn.GetChildren().Any(x => x.GetIntersectionPoint(line).HasValue)) { continue; }
                float intensity = 1.0f - ((world.DistanceFrom(light.Location.ToCbreF())) / light.Range);
                if (intensity < 0.0f) { continue; }
                LineF linef = new LineF(light.Location.ToCbreF(), world);
                if (groupsToRender.AsParallel().SelectMany(x => x.Faces).Any(x => {
                    var val = x.GetIntersectionPoint(linef);
                    return val.HasValue && (val.Value - world).LengthSquared() > LightmapConfig.HitDistanceSquared && (val.Value - light.Location.ToCbreF()).LengthSquared() > LightmapConfig.HitDistanceSquared;
                })) { continue; }
                atlasBuffers[groupToAtlas[group]][uv] += new RenderBuffer.Color {
                    R = intensity * light.Color.X,
                    G = intensity * light.Color.Y,
                    B = intensity * light.Color.Z
                };
            }
            rendered++;
        }
    }
    
    private LightmapGroup[] DetermineGroupsDirectlyAffectedByLightSource(PointLight light) {
        var lightBox = new BoxF(light.BoundingBox);
        return Groups
            .AsParallel()
            .Where(g => g.BoundingBox is not null && g.BoundingBox.IntersectsWith(lightBox))
            .ToArray();
    }
}

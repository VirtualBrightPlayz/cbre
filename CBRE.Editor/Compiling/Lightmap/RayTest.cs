using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CBRE.DataStructures.Geometric;
using CBRE.Graphics;
using CBRE.Settings;
using Microsoft.Xna.Framework;
using Vector3 = System.Numerics.Vector3;

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
    
    public void RenderRayTest() {
        var pointLights = ExtractPointLights();
        var atlases = PrepareAtlases();

        if (Document.MGLightmaps is not null) {
            foreach (var lm in Document.MGLightmaps) {
                lm.Dispose();
            }
            Document.MGLightmaps = null;
        }

        var groupToAtlas = new Dictionary<LightmapGroup, Atlas>();
        foreach (var atlas in atlases) {
            foreach (var group in atlas.Groups) {
                groupToAtlas.Add(group, atlas);
            }
        }
        Document.MGLightmaps ??= new List<ITextureResource>();

        var atlasBuffers = new Dictionary<Atlas, RenderBuffer>();
        foreach (var atlas in atlases) {
            atlasBuffers.Add(atlas, new RenderBuffer());
        }

        foreach (var light in pointLights) {
            RenderLight(light, groupToAtlas, atlasBuffers);
        }

        for (int i=0;i<atlases.Length;i++) {
            var atlas = atlases[i];
            Texture2D texture = new Texture2D(
                GlobalGraphics.GraphicsDevice,
                LightmapConfig.TextureDims,
                LightmapConfig.TextureDims);
                // mipmap: false,
                // SurfaceFormat.Color);
            texture.SetData(atlasBuffers[atlas].SelectMany(p => p.ToRgba32Bytes()).ToArray());
            string fname = System.IO.Path.Combine(typeof(Lightmapper).Assembly.Location, "..", $"lm_{i}.png");
            texture.Name = $"lm_{i}";
            Document.MGLightmaps.Add(texture);
            using var fileStream = File.OpenWrite(fname);
            texture.SaveAsPng(fileStream, texture.Width, texture.Height);
        }
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

        foreach (var group in groupsToRender) {
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
                float intensity
                    = 1.0f - ((getWorldPosFromUv(uv).DistanceFrom(light.Location.ToCbreF())) / light.Range);
                if (intensity <= 0.0f) { continue; }
                atlasBuffers[groupToAtlas[group]][uv] += new RenderBuffer.Color {
                    R = intensity * light.Color.X,
                    G = intensity * light.Color.Y,
                    B = intensity * light.Color.Z
                };
            }
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

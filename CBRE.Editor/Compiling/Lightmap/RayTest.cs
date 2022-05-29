using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CBRE.DataStructures.Geometric;
using CBRE.Settings;
using Microsoft.Xna.Framework;

#nullable enable
namespace CBRE.Editor.Compiling.Lightmap; 

sealed partial class Lightmapper {
    private readonly struct RenderBuffer {
        private readonly Color[] colors; //TODO: use memory-mapped files

        public RenderBuffer() {
            colors = new Color[LightmapConfig.TextureDims * LightmapConfig.TextureDims];
        }

        public Color this[int x, int y] {
            get => colors[x * LightmapConfig.TextureDims + y];
            set => colors[x * LightmapConfig.TextureDims + y] = value;
        }
    }
    
    public void RenderRayTest() {
        var pointLights = ExtractPointLights();
        var atlases = PrepareAtlases();

        var groupToAtlas = new Dictionary<LightmapGroup, Atlas>();
        foreach (var atlas in atlases) {
            foreach (var group in atlas.Groups) {
                groupToAtlas.Add(group, atlas);
            }
        }

        var atlasBuffers = new Dictionary<Atlas, RenderBuffer>();
        foreach (var atlas in atlases) {
            atlasBuffers.Add(atlas, new RenderBuffer());
        }

        foreach (var light in pointLights) {
            RenderLight(light, groupToAtlas, atlasBuffers);
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

            
            
            foreach (var uv in Raster(startUv, endUv)) {
                
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

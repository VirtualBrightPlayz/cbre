#nullable enable
using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using CBRE.Settings;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = System.Drawing.Rectangle;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace CBRE.Editor.Compiling.Lightmap {
    sealed partial class Lightmapper {
        public readonly Document Document;
        public readonly ImmutableArray<LMFace> OpaqueFaces;
        public readonly ImmutableArray<LMFace> TranslucentFaces;
        public readonly ImmutableArray<LMFace> ToolFaces;
        public readonly ImmutableArray<LMFace> UnclassifiedFaces;
        public readonly ImmutableArray<LightmapGroup> Groups;
        
        public Lightmapper(Document document) {
            Document = document;

            var flattenedObjectList = Document.Map.WorldSpawn
                .GetSelfAndAllChildren();
            var solids = flattenedObjectList
                .OfType<Solid>();
            var allFaces = solids.SelectMany(s => s.Faces);
            
            List<LMFace> opaqueFaces = new();
            List<LMFace> translucentFaces = new();
            List<LMFace> toolFaces = new();
            List<LMFace> unclassifiedFaces = new();
            foreach (var face in allFaces) {
                face.UpdateBoundingBox();
                
                LMFace lmFace = new LMFace(face);

                if (lmFace.Texture.Name.StartsWith("ToolTextures/", StringComparison.OrdinalIgnoreCase)) {
                    toolFaces.Add(lmFace);
                } else if (lmFace.Texture.Texture is { } texture) {
                    (texture.HasTransparency() ? translucentFaces : opaqueFaces)
                        .Add(lmFace);
                } else {
                    unclassifiedFaces.Add(lmFace);
                }
            }

            OpaqueFaces = opaqueFaces.ToImmutableArray();
            TranslucentFaces = translucentFaces.ToImmutableArray();
            ToolFaces = toolFaces.ToImmutableArray();
            UnclassifiedFaces = unclassifiedFaces.ToImmutableArray();

            List<LightmapGroup> groups = new();

            foreach (var face in OpaqueFaces) {
                LightmapGroup group = LightmapGroup.FindCoplanar(groups, face);
                if (group is null) {
                    group = new LightmapGroup();
                    groups.Add(group);
                }
                group.AddFace(face);
            }

            Groups = groups.ToImmutableArray();
        }

        public class Atlas : IDisposable {
            public readonly ImmutableHashSet<LightmapGroup> Groups;

            public VertexBuffer GroupVertices { get; private set; }
            public IndexBuffer GroupIndices { get; private set; }
            
            public VertexBuffer GeomVertices { get; private set; }
            public IndexBuffer GeomIndices { get; private set; }

            private int indexCount;

            public Atlas(IEnumerable<LightmapGroup> groups, int lmIndex) {
                Groups = groups.ToImmutableHashSet();
                foreach (var group in Groups) {
                    foreach (var face in group.Faces) {
                        face.UpdateLmUv(group, lmIndex);
                    }
                }
            }

            public void InitGpuBuffers() {
                
                var faces = Groups
                    .SelectMany(g => g.Faces)
                    .ToImmutableArray();
                var vertices = faces
                    .SelectMany(f => f.Vertices)
                    .ToImmutableArray();
                var indices = new List<ushort>();
                long indexOffset = 0;
                foreach (var f in faces) {
                    indices.AddRange(
                        f.GetTriangleIndices()
                            .Select(i => (ushort)(i+indexOffset)));
                    indexOffset += f.Vertices.Length;
                }
                
                var gd = GlobalGraphics.GraphicsDevice;

                GroupVertices = new VertexBuffer(
                    gd,
                    ObjectRenderer.BrushVertex.VertexDeclaration,
                    Groups.Count * 4,
                    BufferUsage.None);
                GroupIndices = new IndexBuffer(
                    gd,
                    IndexElementSize.SixteenBits,
                    Groups.Count * 6,
                    BufferUsage.None);
                
                GroupVertices.SetData(Groups
                    .SelectMany(g => g.GenQuadVerts())
                    .ToArray());
                GroupIndices.SetData(Enumerable.Range(0, Groups.Count)
                    .SelectMany(i => new[] {
                        i*4+0, i*4+1, i*4+2,
                        i*4+2, i*4+3, i*4+1
                    })
                    .Select(i => (ushort)i)
                    .ToArray());
                
                GeomVertices = new VertexBuffer(
                    gd,
                    ObjectRenderer.BrushVertex.VertexDeclaration,
                    vertices.Length,
                    BufferUsage.None);
                GeomIndices = new IndexBuffer(
                    gd,
                    IndexElementSize.SixteenBits,
                    indices.Count,
                    BufferUsage.None);
                
                GeomVertices.SetData(vertices
                    .Select(v => new ObjectRenderer.BrushVertex(v.OriginalVertex))
                    .ToArray());
                GeomIndices.SetData(indices.ToArray());
                indexCount = indices.Count;
            }

            public void RenderGeom() {
                var gd = GlobalGraphics.GraphicsDevice;
                
                gd.SetVertexBuffer(GeomVertices);
                gd.Indices = GeomIndices;
                gd.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,
                    0, 0, indexCount / 3);
            }

            public void RenderGroups() {
                var gd = GlobalGraphics.GraphicsDevice;

                gd.SetVertexBuffer(GroupVertices);
                gd.Indices = GroupIndices;
                gd.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.TriangleList,
                    0, 0, Groups.Count * 2);
            }

            public void Dispose()
            {
                GeomVertices?.Dispose(); GeomVertices = null;
                GeomIndices?.Dispose(); GeomIndices = null;
                GroupVertices?.Dispose(); GroupVertices = null;
                GroupIndices?.Dispose(); GroupIndices = null;
            }

            ~Atlas() {
                Dispose();
            }
        }

        private record PointLight(Vector3 Location, float Range, Vector3 Color) {
            public PointLight(MapObject lightEntity) : this(default, default, default) {
                Location = lightEntity.BoundingBox.Center.ToXna();
                
                var data = lightEntity.GetEntityData();
                float getPropertyFloat(string key)
                    => float.TryParse(data.GetPropertyValue(key), NumberStyles.Any, CultureInfo.InvariantCulture,
                        out float v)
                        ? v
                        : 0.0f;
                
                Range = getPropertyFloat("range");
                Color = data.GetPropertyVector3("color").ToXna() * getPropertyFloat("intensity") / 255.0f;
            }

            public Box BoundingBox => new(
                (Location - new Vector3(Range, Range, Range)).ToCbre(),
                (Location + new Vector3(Range, Range, Range)).ToCbre());
        }
        
        private ImmutableArray<PointLight> ExtractPointLights()
            => Document.Map.WorldSpawn.Find(
                    e => string.Equals(e.GetEntityData()?.Name, "light", StringComparison.OrdinalIgnoreCase))
                .Select(e => new PointLight(e))
                .ToImmutableArray();

        private ImmutableArray<Atlas> PrepareAtlases() {
            List<LightmapGroup> remainingGroups = Groups
                .OrderByDescending(g => g.WorldSpaceWidth * g.WorldSpaceHeight)
                .ThenByDescending(g => g.WorldSpaceWidth)
                .ThenByDescending(g => g.WorldSpaceHeight)
                .ToList();

            List<Atlas> atlases = new();

            while (remainingGroups.Any()) {
                int prevCount = remainingGroups.Count;
                
                var prevGroups = remainingGroups.ToArray();
                CalculateUv(
                    remainingGroups,
                    new Rectangle(
                        1,
                        1,
                        LightmapConfig.TextureDims-2,
                        LightmapConfig.TextureDims-2),
                    out _,
                    out _);

                if (prevCount == remainingGroups.Count) {
                    throw new Exception(
                        $"{prevCount} lightmap groups do not fit within the given resolution and downscale factor");
                }

                var newAtlas = new Atlas(prevGroups.Where(g => !remainingGroups.Contains(g)), atlases.Count);
                atlases.Add(newAtlas);
            }
            
            return atlases.ToImmutableArray();
        }
        
        

        private static void CalculateUv(
            List<LightmapGroup> lmGroups,
            Rectangle area,
            out int usedWidth,
            out int usedHeight
        ) {
            usedWidth = 0;
            usedHeight = 0;
            if (lmGroups.Count <= 0) { return; }

            for (int i = 0; i < lmGroups.Count; i++) {
                LightmapGroup lmGroup = lmGroups[i];

                //Make the aspect ratio of the group
                //closer to the aspect ratio of the
                //available area, since this gives
                //better odds of the group fitting
                if ((area.Width <= area.Height) != (lmGroup.WorldSpaceWidth <= lmGroup.WorldSpaceHeight)) {
                    lmGroup.SwapUv();
                }

                int downscaledWidth = 0;
                int downscaledHeight = 0;
                bool fits = false;
                
                for (int attempts = 0; attempts < 2; attempts++) {
                    downscaledWidth = (int)Math.Ceiling(lmGroup.UvSpaceWidth);
                    downscaledHeight = (int)Math.Ceiling(lmGroup.UvSpaceHeight);

                    if (downscaledWidth > area.Width || downscaledHeight > area.Height) {
                        //The group did not fit, try flipping the group
                        //because it might be able to fit that way
                        lmGroup.SwapUv();
                    } else {
                        fits = true;
                        break;
                    }
                }

                if (!fits) { continue; } //The given group simply does not fit in the given area, try the next one

                lmGroups.RemoveAt(i); //Remove the current group from the list of pending groups
                
                lmGroup.StartWriteUV.U = area.Left;
                lmGroup.StartWriteUV.V = area.Top;
                usedWidth += downscaledWidth;
                usedHeight += downscaledHeight;
                
                //There are now four regions that are considered to introduce more groups:
                //  XXXXXXXX | AAAAAAAA
                //  XXXXXXXX | AAAAAAAA
                //  XXXXXXXX | AAAAAAAA
                //  -------------------
                //  BBBBBBBB | CCCCCCCC
                //  BBBBBBBB | CCCCCCCC
                //  BBBBBBBB | CCCCCCCC
                //
                //Region X is completely taken up by the current group.
                //Regions A, B and C are extra space that should be filled,
                //their dimensions are based on the size of region X

                //Try to fill region A
                if (downscaledWidth < area.Width) {
                    int subWidth = -1;
                    usedWidth += LightmapConfig.PlaneMargin;
                    while (subWidth != 0) {
                        CalculateUv(lmGroups, new Rectangle(
                                area.Left + usedWidth,
                                area.Top,
                                area.Width - usedWidth,
                                downscaledHeight),
                            out subWidth, out _);
                        usedWidth += subWidth + LightmapConfig.PlaneMargin;
                    }
                }

                //Try to fill region B
                if (downscaledHeight < area.Height) {
                    int subHeight = -1;
                    usedHeight += LightmapConfig.PlaneMargin;
                    while (subHeight != 0) {
                        CalculateUv(lmGroups, new Rectangle(
                                area.Left,
                                area.Top + usedHeight,
                                downscaledWidth,
                                area.Height - usedHeight),
                            out _, out subHeight);
                        usedHeight += subHeight + LightmapConfig.PlaneMargin;
                    }
                }

                //Try to fill region C
                if (downscaledWidth < area.Width && downscaledHeight < area.Height) {
                    Rectangle remainder = new Rectangle(
                        area.Left + downscaledWidth + LightmapConfig.PlaneMargin,
                        area.Top + downscaledHeight + LightmapConfig.PlaneMargin,
                        area.Width - downscaledWidth - LightmapConfig.PlaneMargin,
                        area.Height - downscaledHeight - LightmapConfig.PlaneMargin);

                    CalculateUv(lmGroups, remainder,
                        out int subWidth, out int subHeight);

                    usedWidth = Math.Max(usedWidth, downscaledWidth + LightmapConfig.PlaneMargin + subWidth);
                    usedHeight = Math.Max(usedHeight, downscaledHeight + LightmapConfig.PlaneMargin + subHeight);
                }

                //We managed to fit at least one group, we're done here!
                return;
            }
        }
    }
}

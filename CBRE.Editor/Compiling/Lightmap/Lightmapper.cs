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
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CBRE.Editor.Compiling.Lightmap {
    sealed class Lightmapper {
        public readonly Document Document;
        public readonly ImmutableHashSet<LMFace> OpaqueFaces;
        public readonly ImmutableHashSet<LMFace> TranslucentFaces;
        public readonly ImmutableHashSet<LMFace> ToolFaces;
        public readonly ImmutableHashSet<LMFace> UnclassifiedFaces;
        public readonly ImmutableHashSet<LightmapGroup> Groups;
        
        public Lightmapper(Document document) {
            Document = document;

            var flattenedObjectList = Document.Map.WorldSpawn
                .GetSelfAndAllChildren();
            var solids = flattenedObjectList
                .OfType<Solid>();
            var allFaces = solids.SelectMany(s => s.Faces);
            
            HashSet<LMFace> opaqueFaces = new();
            HashSet<LMFace> translucentFaces = new();
            HashSet<LMFace> toolFaces = new();
            HashSet<LMFace> unclassifiedFaces = new();
            foreach (var face in allFaces) {
                face.Vertices.ForEach(v => { v.LMU = -500.0f; v.LMV = -500.0f; });
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

            OpaqueFaces = opaqueFaces.ToImmutableHashSet();
            TranslucentFaces = translucentFaces.ToImmutableHashSet();
            ToolFaces = toolFaces.ToImmutableHashSet();
            UnclassifiedFaces = unclassifiedFaces.ToImmutableHashSet();

            HashSet<LightmapGroup> groups = new();

            foreach (var face in OpaqueFaces) {
                LightmapGroup group = LightmapGroup.FindCoplanar(groups, face);
                if (group is null) {
                    group = new LightmapGroup();
                    groups.Add(group);
                }
                group.AddFace(face);
            }

            Groups = groups.ToImmutableHashSet();
        }

        private static void CalculateUv(
            List<LightmapGroup> lmGroups,
            Rectangle area,
            out int usedWidth,
            out int usedHeight) {
            
            usedWidth = 0;
            usedHeight = 0;
            if (lmGroups.Count <= 0) { return; }

            for (int i = 0; i < lmGroups.Count; i++) {
                LightmapGroup lmGroup = lmGroups[i];

                if ((area.Width <= area.Height) != (lmGroup.Width <= lmGroup.Height)) {
                    lmGroup.SwapUv();
                }

                int downscaledWidth = 0;
                int downscaledHeight = 0;
                bool fits = false;
                
                for (int attempts = 0; attempts < 2; attempts++) {
                    downscaledWidth = (int)Math.Ceiling(lmGroup.Width / LightmapConfig.DownscaleFactor);
                    downscaledHeight = (int)Math.Ceiling(lmGroup.Height / LightmapConfig.DownscaleFactor);

                    if (downscaledWidth > area.Width || downscaledHeight > area.Height) {
                        lmGroup.SwapUv();
                    } else {
                        fits = true;
                        break;
                    }
                }

                if (!fits) { return; }

                usedWidth += downscaledWidth;
                usedHeight += downscaledHeight;
                lmGroups.RemoveAt(i);
                lmGroup.WriteU = area.Left;
                lmGroup.WriteV = area.Top;

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

                if (downscaledHeight < area.Height) {
                    int subHeight = -1;
                    usedHeight += LightmapConfig.PlaneMargin;
                    while (subHeight != 0) {
                        CalculateUv(lmGroups, new Rectangle(area.Left,
                                area.Top + usedHeight,
                                downscaledWidth,
                                area.Height - usedHeight),
                            out _, out subHeight);
                        usedHeight += subHeight + LightmapConfig.PlaneMargin;
                    }
                }

                if (downscaledWidth < area.Width && downscaledHeight < area.Height) {
                    Rectangle remainder = new Rectangle(
                        area.Left + downscaledWidth + LightmapConfig.PlaneMargin,
                        area.Top + downscaledHeight + LightmapConfig.PlaneMargin,
                        area.Width - downscaledWidth - LightmapConfig.PlaneMargin,
                        area.Height - downscaledHeight - LightmapConfig.PlaneMargin);

                    CalculateUv(lmGroups, remainder,
                        out int subWidth, out int subHeight);

                    usedWidth += subWidth;
                    usedHeight += subHeight;
                }
            }
        }
    }
    
    /*
        List<LightmapGroup> uvCalcFaces = new List<LightmapGroup>(lmGroups);

        int totalTextureDims = LightmapConfig.TextureDims;
        lmCount = 0;
        for (int i = 0; i < 4; i++) {
            int x = 1 + ((i % 2) * LightmapConfig.TextureDims);
            int y = 1 + ((i / 2) * LightmapConfig.TextureDims);
            CalculateUV(uvCalcFaces, new Rectangle(x, y, LightmapConfig.TextureDims - 2, LightmapConfig.TextureDims - 2), out _, out _);
            lmCount++;
            if (uvCalcFaces.Count == 0) { break; }
            totalTextureDims = LightmapConfig.TextureDims * 2;
        }
    */
}

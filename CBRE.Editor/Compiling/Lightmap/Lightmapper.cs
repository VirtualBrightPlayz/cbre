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
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CBRE.Editor.Compiling.Lightmap {
    static class Lightmapper {
        private static void CalculateUV(
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

                for (int j = 0; j < 2; j++) {
                    int downscaledWidth = (int)Math.Ceiling(lmGroup.Width / LightmapConfig.DownscaleFactor);
                    int downscaledHeight = (int)Math.Ceiling(lmGroup.Height / LightmapConfig.DownscaleFactor);

                    if (downscaledWidth <= area.Width && downscaledHeight <= area.Height) {
                        usedWidth += downscaledWidth;
                        usedHeight += downscaledHeight;
                        lmGroups.RemoveAt(i);
                        lmGroup.WriteU = area.Left;
                        lmGroup.WriteV = area.Top;

                        int subWidth = -1;
                        int subHeight = -1;
                        if (downscaledWidth < area.Width) {
                            int subUsedWidth = 0;
                            while (subWidth != 0) {
                                CalculateUV(lmGroups, new Rectangle(
                                        area.Left + subUsedWidth + downscaledWidth + LightmapConfig.PlaneMargin,
                                        area.Top,
                                        area.Width - subUsedWidth - downscaledWidth - LightmapConfig.PlaneMargin,
                                        downscaledHeight),
                                    out subWidth, out subHeight);
                                subUsedWidth += subWidth + LightmapConfig.PlaneMargin;
                            }

                            usedWidth += subUsedWidth;
                            subWidth = -1;
                            subHeight = -1;
                        }

                        if (downscaledHeight < area.Height) {
                            int subUsedHeight = 0;
                            while (subHeight != 0) {
                                CalculateUV(lmGroups, new Rectangle(area.Left,
                                        area.Top + subUsedHeight + downscaledHeight + LightmapConfig.PlaneMargin,
                                        downscaledWidth,
                                        area.Height - subUsedHeight - downscaledHeight - LightmapConfig.PlaneMargin),
                                    out subWidth, out subHeight);
                                subUsedHeight += subHeight + LightmapConfig.PlaneMargin;
                            }

                            usedHeight += subUsedHeight;
                        }

                        if (downscaledWidth < area.Width && downscaledHeight < area.Height) {
                            Rectangle remainder = new Rectangle(
                                area.Left + downscaledWidth + LightmapConfig.PlaneMargin,
                                area.Top + downscaledHeight + LightmapConfig.PlaneMargin,
                                area.Width - downscaledWidth - LightmapConfig.PlaneMargin,
                                area.Height - downscaledHeight - LightmapConfig.PlaneMargin);

                            CalculateUV(lmGroups, remainder,
                                out subWidth, out subHeight);

                            usedWidth += subWidth;
                            usedHeight += subHeight;
                        }

                        return;
                    }

                    lmGroup.SwapUv();
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

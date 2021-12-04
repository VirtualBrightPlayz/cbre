#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CBRE.Graphics;
using CBRE.Providers.Texture;

namespace CBRE.Editor {
    public static class DebugStats {
        private static Process? proc = null;

        public static IEnumerable<string> Get() {
            proc ??= Process.GetCurrentProcess();
            proc.Refresh();

            string bytesToMiB(long byteCount)
                => $"{(byteCount / 1024.0f / 1024.0f): 0.0} MiB";

            yield return ($"Working set: {bytesToMiB(proc.WorkingSet64)}");
            yield return ($"Private mem: {bytesToMiB(proc.PrivateMemorySize64)}");
            yield return ($"Paged mem: {bytesToMiB(proc.PagedMemorySize64)}");
            yield return ($"GC.GetTotalMemory: {bytesToMiB(GC.GetTotalMemory(false))}");
            yield return ($"GC.GetTotalAllocatedBytes: {bytesToMiB(GC.GetTotalAllocatedBytes(false))}");
            yield return ($"Gen 0 collections: {GC.CollectionCount(0)}");
            yield return ($"Gen 1 collections: {GC.CollectionCount(1)}");
            yield return ($"Gen 2 collections: {GC.CollectionCount(2)}");
            yield return ($"Textures loaded: {TextureProvider.Packages.Sum(p => p.Items.Count())}");
            yield return ($"Texture buffer size total: "
                          + bytesToMiB(TextureProvider.Packages.Sum(
                              p => p.Items.Sum(
                                  i => i.Value.Texture is AsyncTexture { DataBufferSize: int bufferSz } ? bufferSz : 0))));
            yield return ($"Loading texture buffer size total: "
                          + bytesToMiB(TextureProvider.Packages.Sum(
                              p => p.Items.Sum(
                                  i => i.Value.Texture is AsyncTexture { LoadingBufferSize: int bufferSz } ? bufferSz : 0))));
        }
    }
}

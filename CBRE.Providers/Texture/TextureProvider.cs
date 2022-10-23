using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CBRE.Common;

namespace CBRE.Providers.Texture {
    public static class TextureProvider {
        public static readonly List<TexturePackage> Packages;

        public static TextureItem SelectedTexture;

        static TextureProvider() {
            Packages = new List<TexturePackage>();
        }

        public static void SetCachePath(string path) {
            CachePath = path;
        }

        public struct TextureCategory {
            public string Path;
            public string CategoryName;
            public string Prefix;
        }

        private static string CachePath;
        public static void CreatePackages(IEnumerable<TextureCategory> sourceRoots) {
            var dirs = sourceRoots.Where(sr => Directory.Exists(sr.Path));

            foreach (var dir in dirs) {
                var tp = new TexturePackage(dir.Path, dir.CategoryName);

                var sprs = Directory.GetFiles(dir.Path, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".png"));
                if (!sprs.Any()) continue;

                foreach (var spr in sprs) {
                    var rel = Path.GetFullPath(spr).Substring(dir.Path.Length).TrimStart('/', '\\').Replace('\\', '/');
                    rel = rel.Replace(".jpg", "").Replace(".jpeg", "").Replace(".png", "").ToLowerInvariant();
                    rel = dir.Prefix + rel;

                    tp.AddTexture(new TextureItem(tp, rel.ToLowerInvariant(), Path.GetFullPath(spr)));
                }
                if (tp.Items.Any()) { Packages.Add(tp); }
            }
        }

        public static void DeletePackages(IEnumerable<TexturePackage> packages) { }

        public static TextureItem GetItem(string name) {
            string lowerName = name.ToLowerInvariant();
            foreach (var p in Packages) {
                if (p.Items.ContainsKey(lowerName)) { return p.Items[lowerName]; }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Providers.Texture {
    public abstract class TextureProvider {
        private static readonly List<TextureProvider> RegisteredProviders;
        private static readonly List<TextureCollection> Collections;
        private static readonly List<TexturePackage> Packages;

        static TextureProvider() {
            RegisteredProviders = new List<TextureProvider>();
            Collections = new List<TextureCollection>();
            Packages = new List<TexturePackage>();
        }

        private static string _cachePath;

        public static void SetCachePath(string path) {
            _cachePath = path;
            foreach (var p in RegisteredProviders) p.CachePath = _cachePath;
        }

        #region Registration

        public static void Register(TextureProvider provider) {
            provider.CachePath = _cachePath;
            RegisteredProviders.Add(provider);
        }

        public static void Deregister(TextureProvider provider) {
            RegisteredProviders.Remove(provider);
        }

        #endregion

        public struct TextureCategory {
            public string Path;
            public string CategoryName;
            public string Prefix;
        }

        protected string CachePath { get; private set; }
        public abstract IEnumerable<TexturePackage> CreatePackages(IEnumerable<TextureCategory> sourceRoots);
        public abstract void DeletePackages(IEnumerable<TexturePackage> packages);
        public abstract void LoadTextures(IEnumerable<TextureItem> items);
        public abstract ITextureStreamSource GetStreamSource(int maxWidth, int maxHeight, IEnumerable<TexturePackage> packages);

        public static TextureCollection CreateCollection(IEnumerable<TextureCategory> sourceRoots) {
            var list = sourceRoots.ToList();
            var pkgs = new List<TexturePackage>();
            foreach (var provider in RegisteredProviders) {
                pkgs.AddRange(provider.CreatePackages(list));
            }

            var tc = new TextureCollection(pkgs);
            Packages.AddRange(pkgs);
            Collections.Add(tc);
            return tc;
        }

        public static void DeleteCollection(TextureCollection collection) {
            Collections.RemoveAll(x => x == collection);
            var remove = Packages.Where(package => !Collections.Any(x => x.Packages.Contains(package))).ToList();
            foreach (var package in remove) {
                Packages.Remove(package);
                package.Provider.DeletePackages(new[] { package });
                package.Dispose();
            }
        }

        public static void LoadTextureItem(TextureItem item) {
            if (item == null || item.Package == null) return;
            item.Package.Provider.LoadTextures(new[] { item });
        }

        public static void LoadTextureItems(IEnumerable<TextureItem> items) {
            var list = items.ToList();

            foreach (var g in list.GroupBy(x => x.Package.Provider)) {
                LoadTextures(g.Key, g);
            }
        }

        private static void LoadTextures(TextureProvider provider, IEnumerable<TextureItem> items) {
            var all = items.ToList();
            if (!all.Any()) return;
            provider.LoadTextures(all);
        }
    }
}

using CBRE.FileSystem;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Settings.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CBRE.Editor.Environment {
    public class GameEnvironment {
        public Game Game { get; private set; }

        public Build Build {
            get {
                return SettingsManager.Build;
            }
        }

        private IFile _root;

        public IFile Root {
            get {
                if (_root == null) {
                    var dirs = GetGameDirectories().Where(Directory.Exists).ToList();
                    if (dirs.Any()) _root = new RootFile(Game.Name, dirs.Select(x => new NativeFile(x)));
                    else _root = new VirtualFile(null, "");
                }
                return _root;
            }
        }

        public GameEnvironment(Game game) {
            Game = game;
        }

        public IEnumerable<TextureProvider.TextureCategory> GetTextureCategories() {
            for (int i = 0; i < Directories.TextureDirs.Count; i++) {
                string dir = Directories.TextureDirs[i];
                yield return new TextureProvider.TextureCategory {
                    Path = dir,
                    CategoryName = $"Texture dir {i}"
                };
            }

            string exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            yield return new TextureProvider.TextureCategory {
                Path = Path.Combine(exeDir, "ToolTextures"),
                CategoryName = "Tool textures",
                Prefix = "tooltextures/"
            };
            yield return new TextureProvider.TextureCategory {
                Path = Path.Combine(exeDir, "Sprites"),
                CategoryName = "Sprites",
                Prefix = "sprites/"
            };
        }

        public IEnumerable<string> GetGameDirectories() {
            foreach (string dir in Directories.TextureDirs) {
                yield return dir;
            }
            foreach (string dir in Directories.ModelDirs) {
                yield return dir;
            }

            var b = Build;
            if (b != null && b.IncludePathInEnvironment) {
                yield return b.Path;
            }

            // Editor location to the path, for sprites and the like
            yield return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
    }
}

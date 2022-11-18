using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CBRE.Providers.Texture;

namespace CBRE.Settings {
    public class Directories {
        public static List<string> TextureDirs { get; set; }
        public static List<string> ModelDirs { get; set; }

        static Directories() {
            TextureDirs = new List<string>();
            ModelDirs = new List<string>();
        }

        private static readonly string[] TextureExtensions = { "jpeg", "jpg", "png" };
        private static readonly string[] ModelExtensions = { "fbx", "x", "b3d" };

        public static string GetTextureExtension(string filename) {
            foreach (string dir in TextureDirs) {
                string dirSlash = dir;
                if (dir.Last() != '/' && dir.Last() != '\\') {
                    dirSlash += "/";
                }
                foreach (string ext in TextureExtensions) {
                    string fileWithExt = filename + "." + ext;
                    if (File.Exists(dirSlash + fileWithExt)) { return fileWithExt; }
                }
            }
            return filename;
        }

        public static string GetModelPath(string filename) {
            foreach (string dir in ModelDirs) {
                string dirSlash = dir;
                if (dir.Last() != '/' && dir.Last() != '\\') {
                    dirSlash += "/";
                }
                if (File.Exists(dirSlash + filename)) { return dirSlash + filename; }
                foreach (string ext in ModelExtensions) {
                    string fullFilename = dirSlash + filename + "." + ext;
                    if (File.Exists(fullFilename)) { return fullFilename; }
                }
            }
            return null;
        }

        public static IEnumerable<TextureProvider.TextureCategory> GetTextureCategories() {
            for (int i = 0; i < TextureDirs.Count; i++) {
                string dir = TextureDirs[i];
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
    }
}

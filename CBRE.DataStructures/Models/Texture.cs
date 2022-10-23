using CBRE.Common;

namespace CBRE.DataStructures.Models {
    public class Texture {
        public int Index { get; set; }
        public string Name { get; set; }
        public ITexture TextureObject { get; set; }
        public int Width { get { return (int)TextureObject.UWidth; } }
        public int Height { get { return (int)TextureObject.UHeight; } }
        public int Flags { get; set; }
    }
}

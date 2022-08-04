using CBRE.Common;

namespace CBRE.DataStructures.Models {
    public class Texture {
        public int Index { get; set; }
        public string Name { get; set; }
        public ITexture TextureObject { get; set; }
        public int Width { get { return TextureObject.UWidth; } }
        public int Height { get { return TextureObject.UHeight; } }
        public int Flags { get; set; }
    }
}

using CBRE.Common;
using System.Drawing;
using System.Drawing.Imaging;

namespace CBRE.Providers.Texture {
    public class NullTextureStreamSource : ITextureStreamSource {
        static NullTextureStreamSource() {
            
        }

        private readonly int _maxWidth;
        private readonly int _maxHeight;

        public NullTextureStreamSource(int maxWidth, int maxHeight) {
            _maxWidth = maxWidth;
            _maxHeight = maxHeight;
        }

        public bool HasImage(TextureItem item) {
            return item.Texture != null;
        }

        public void Dispose() {

        }
    }
}

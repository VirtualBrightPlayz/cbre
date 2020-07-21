using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CBRE.Providers.Texture {
    public interface ITextureStreamSource : IDisposable {
        bool HasImage(TextureItem item);
    }

    public class MultiTextureStreamSource : ITextureStreamSource {
        private readonly List<ITextureStreamSource> _streams;

        public MultiTextureStreamSource(IEnumerable<ITextureStreamSource> streams) {
            _streams = streams.ToList();
        }

        public bool HasImage(TextureItem item) {
            return _streams.Any(x => x.HasImage(item));
        }

        public void Dispose() {
            _streams.ForEach(x => x.Dispose());
        }
    }
}

using CBRE.Common;
using CBRE.Graphics;
using System.Collections.Generic;

namespace CBRE.Providers.Texture {
    public class TextureItem {
        public TexturePackage Package { get; private set; }
        public string Name { get; private set; }
        public string Filename { get; private set; }

        public TextureSubItem PrimarySubItem {
            get { return _subItems.ContainsKey(TextureSubItemType.Base) ? _subItems[TextureSubItemType.Base] : null; }
        }

        private readonly Dictionary<TextureSubItemType, TextureSubItem> _subItems;

        public IEnumerable<TextureSubItem> AllSubItems {
            get { return _subItems.Values; }
        }

        public int Width { get { return PrimarySubItem.Width; } }
        public int Height { get { return PrimarySubItem.Height; } }

        public readonly ITexture Texture;

        public TextureItem(TexturePackage package, string name, string filename) {
            Package = package;
            Name = name;
            Filename = filename;
            Texture = new AsyncTexture(filename);
            _subItems = new Dictionary<TextureSubItemType, TextureSubItem>();
        }

        public TextureSubItem AddSubItem(TextureSubItemType type, string name, int width, int height) {
            var si = new TextureSubItem(type, this, name, width, height);
            _subItems.Add(type, si);
            return si;
        }
    }
}

using OpenTK;
using OpenTK.Graphics;

namespace CBRE.Editor.Rendering.Arrays {
    public struct MapObjectVertex {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Texture;
        public Vector2 LightmapUv;
        public Color4 Colour;
        public float IsSelected;
    }
}
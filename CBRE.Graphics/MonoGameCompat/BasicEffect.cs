using System.Numerics;

namespace CBRE.Graphics {
    public class BasicEffect : Effect {
        public Matrix4x4 World {
            get => Parameters["World"].GetValue<Matrix4x4>();
            set => Parameters["World"].SetValue(value);
        }
        public Matrix4x4 View {
            get => Parameters["View"].GetValue<Matrix4x4>();
            set => Parameters["View"].SetValue(value);
        }
        public Matrix4x4 Projection {
            get => Parameters["Projection"].GetValue<Matrix4x4>();
            set => Parameters["Projection"].SetValue(value);
        }
        public Veldrid.Texture Texture {
            get => Parameters["Texture"].GetValue<Veldrid.Texture>();
            set => Parameters["Texture"].SetValue(value);
        }
        public Vector3 DiffuseColor {
            get => Parameters["DiffuseColor"].GetValue<Vector3>();
            set => Parameters["DiffuseColor"].SetValue(value);
        }
        #region TODO
        public bool TextureEnabled { get; set; }
        public bool VertexColorEnabled { get; set; }
        #endregion TODO

        public BasicEffect() : base("Shaders/basic") {
        }
    }
}
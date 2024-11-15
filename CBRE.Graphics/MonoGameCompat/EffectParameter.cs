using System.Numerics;
using Veldrid;

namespace CBRE.Graphics {
    public class EffectParameter {
        private object _value;
        public GraphicsResource Value { get; private set; }
        public BindableResource[] Bindables { get; private set; }
        public bool IsDirty { get; set; } = false;

        public T GetValue<T>() {
            return (T)_value;
        }

        public void SetValue<T>(T val) where T : unmanaged {
            IsDirty = true;
            UniformBuffer buf = new UniformBuffer();
            buf.SetData<T>(val);
            _value = val;
            Value = buf;
            Bindables = new BindableResource[] { buf._buffer };
        }

        public void SetValue(Texture2D val) {
            IsDirty = true;
            _value = val;
            Value = val;
            Bindables = new BindableResource[] { val._texture, GlobalGraphics.PointSampler }; // TODO: samplers
        }

        public void SetValue(Texture val) {
            IsDirty = true;
            _value = Value = new Texture2D(val);
            Bindables = new BindableResource[] { val, GlobalGraphics.PointSampler }; // TODO: samplers
        }

        public void SetValue(ITextureResource val) {
            SetValue(val.GetInternalTexture());
        }

        public void SetValue(RenderTarget2D val) {
            IsDirty = true;
            _value = val;
            Value = val;
            Bindables = new BindableResource[] { val._texture, val._depth, GlobalGraphics.PointSampler }; // TODO: samplers
        }

        public void SetValue(AsyncTexture val) {
            IsDirty = true;
            Bindables = new BindableResource[] { val.VeldridTexture, GlobalGraphics.PointSampler }; // TODO: samplers
        }
    }
}
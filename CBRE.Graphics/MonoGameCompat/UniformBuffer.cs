using Veldrid;

namespace CBRE.Graphics {
    public class UniformBuffer : GraphicsResource {
        internal DeviceBuffer _buffer = null;
        internal uint _size = 0;

        public UniformBuffer() {
        }

        public unsafe void SetData<T>(T data) where T : unmanaged {
            if (_buffer != null && _size != sizeof(T)) {
                _buffer?.Dispose();
                BufferDescription desc = new BufferDescription(_size, Veldrid.BufferUsage.UniformBuffer);
                _buffer = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateBuffer(desc);
            }
            GlobalGraphics.GraphicsDevice.UpdateBuffer<T>(_buffer, 0, data);
        }

        public override void Dispose() {
            _buffer?.Dispose();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;

namespace CBRE.Graphics {
    public class IndexBuffer : GraphicsResource {
        internal DeviceBuffer _buffer;
        internal readonly Veldrid.IndexFormat _indexFormat;

        public IndexBuffer(GraphicsDevice gd, IndexElementSize size, int sizeInBytes, BufferUsage usage) : this(size, sizeInBytes, usage) {
        }

        public IndexBuffer(IndexElementSize size, int sizeInBytes, BufferUsage usage) {
            switch (size) {
                case IndexElementSize.SixteenBits:
                    _indexFormat = Veldrid.IndexFormat.UInt16;
                    break;
                case IndexElementSize.ThirtyTwoBits:
                    _indexFormat = Veldrid.IndexFormat.UInt32;
                    break;
            }
            BufferDescription desc = new BufferDescription((uint)size, Veldrid.BufferUsage.IndexBuffer);
            _buffer = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateBuffer(desc);
        }

        public void SetData<T>(T[] data, int offset, int size) where T : struct {
            GlobalGraphics.GraphicsDevice.UpdateBuffer<T>(_buffer, (uint)offset, data);
        }

        public void SetData<T>(T[] data) where T : struct {
            SetData<T>(data, 0, data.Length);
        }

        public override void Dispose() {
            _buffer?.Dispose();
        }
    }

    public enum IndexElementSize : byte {
        SixteenBits,
        ThirtyTwoBits,
    }
}

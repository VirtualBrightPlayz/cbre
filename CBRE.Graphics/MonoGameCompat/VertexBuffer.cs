using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;

namespace CBRE.Graphics {
    public class VertexBuffer : GraphicsResource {
        internal DeviceBuffer _buffer;
        internal VertexDeclaration _declaration;
        public readonly int VertexCount;

        public VertexBuffer(GraphicsDevice gd, VertexDeclaration declaration, int size, BufferUsage usage) : this(declaration, size, usage) {
        }

        public VertexBuffer(GraphicsDevice gd, Type declaration, int size, BufferUsage usage) : this(default(VertexDeclaration), size, usage) {
        }

        public VertexBuffer(VertexDeclaration declaration, int size, BufferUsage usage) {
            VertexCount = size;
            _declaration = declaration;
            BufferDescription desc = new BufferDescription((uint)size, Veldrid.BufferUsage.VertexBuffer);
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

    public class VertexDeclaration {
        public VertexElement[] elements;

        public VertexDeclaration(VertexElement[] elements) {
            this.elements = elements;
        }

        public VertexLayoutDescription ToVeldrid() {
            return new VertexLayoutDescription(elements.Select(x => x.ToVeldrid()).ToArray());
        }
    }

    public class VertexElement {
        public readonly VertexElementFormat Format;
        public readonly VertexElementUsage Usage;

        public VertexElement(int offset, CBRE.Graphics.VertexElementFormat format, CBRE.Graphics.VertexElementUsage description, int stride) {
            Format = format;
            Usage = description;
        }

        public VertexElementDescription ToVeldrid() {
            return new VertexElementDescription();
        }
    }

    public enum VertexElementFormat : byte {
        Single,
        Vector2,
        Vector3,
        Vector4,
        Color,
    }

    public enum VertexElementUsage : byte {
        Position,
        Normal,
        TextureCoordinate,
        Color,
    }

    public enum BufferUsage : byte {
        None,
        WriteOnly,
    }
}

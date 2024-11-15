using System;
using System.Collections.Generic;
using System.Text;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBREVector3 = CBRE.DataStructures.Geometric.Vector3;
using CBREMatrix = CBRE.DataStructures.Geometric.Matrix;
using Veldrid;
using System.Numerics;
using Num = System.Numerics;
using Veldrid.SPIRV;
using System.IO;

namespace CBRE.Graphics {

    public enum PrimitiveType : byte {
        TriangleList = 0,
        TriangleStrip = 1,
        LineList = 2,
        LineStrip = 3,
        PointList = 4,
        QuadList = 5,
        LineLoop = 6,
    }

    public static class PrimitiveDrawing {
        public static bool IsLineType(this PrimitiveType type)
            => type is PrimitiveType.LineList or PrimitiveType.LineStrip;

        public static bool IsTriangleType(this PrimitiveType type)
            => type is PrimitiveType.TriangleList or PrimitiveType.TriangleStrip;

        private static PrimitiveType? currentPrimitiveType = null;
        private static DeviceBuffer vertexBuffer = null;
        private static Shader[] shaders = null;
        private static VertexFragmentCompilationResult shaderCompilerResult = null;

        private static Vector4 color = Vector4.One;
        private static List<VertexPositionColorTexture> vertices = new List<VertexPositionColorTexture>();
        public static Texture Texture = null;

        public static void Begin(PrimitiveType primType) {
            if (currentPrimitiveType != null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Begin because a draw operation is already in progress"); }
            currentPrimitiveType = primType;
            vertices.Clear();
        }

        public static void SetColor(System.Drawing.Color clr) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Color4 because a draw operation isn't in progress"); }
            color.X = clr.R;
            color.Y = clr.G;
            color.Z = clr.B;
            color.W = clr.A;
        }

        public static void Vertex2(double x, double y, float u = 0f, float v = 0f) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Vertex3 because a draw operation isn't in progress"); }
            vertices.Add(new VertexPositionColorTexture() {
                Position = new Num.Vector3((float)x, (float)y, 0.0f),
                Color = new RgbaFloat(color),
                TextureCoordinate = new Vector2(u, v)
            });
        }

        public static void Vertex3(Num.Vector3 position, float u = 0f, float v = 0f) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Vertex3 because a draw operation isn't in progress"); }
            vertices.Add(new VertexPositionColorTexture() {
                Position = position,
                Color = new RgbaFloat(color),
                TextureCoordinate = new Vector2(u, v)
            });
        }

        public static void Vertex3(double x, double y, double z, float u = 0f, float v = 0f) {
            Vertex3(new Num.Vector3((float)x, (float)y, (float)z), u, v);
        }

        public static void Vertex3(CBRE.DataStructures.Geometric.Vector3 position, float u = 0f, float v = 0f) {
            Vertex3(position.DX, position.DY, position.DZ, u, v);
        }

        public static void DottedLine(CBRE.DataStructures.Geometric.Vector3 pos0, CBRE.DataStructures.Geometric.Vector3 pos1, decimal subLen) {
            decimal len = (pos1 - pos0).VectorMagnitude();
            CBRE.DataStructures.Geometric.Vector3 vec = (pos1 - pos0) / len;
            decimal acc = 0m;
            while (acc < len) {
                Vertex3(pos0 + vec * acc);
                acc += subLen;
                if (acc < len) {
                    Vertex3(pos0 + vec * acc);
                } else {
                    Vertex3(pos1);
                }
                acc += subLen;
            }
        }

        public static void Circle(CBRE.DataStructures.Geometric.Vector3 position, double radius) {
            for (int i = 0; i < 12; i++) {
                double cx = Math.Cos((double)i * Math.PI * 2.0 / 12.0) * radius;
                double cy = Math.Sin((double)i * Math.PI * 2.0 / 12.0) * radius;
                Vertex3(position.DX + cx, position.DY + cy, position.DZ);
            }
        }

        public static void Square(CBREVector3 position, decimal radius)
            => Square(position, (double)radius);

        public static void Square(CBRE.DataStructures.Geometric.Vector3 position, double radius) {
            for (int i = 0; i < 4; i++) {
                double cx = Math.Cos(((double)i + 0.5f) * Math.PI * 2.0 / 4.0) * radius;
                double cy = Math.Sin(((double)i + 0.5f) * Math.PI * 2.0 / 4.0) * radius;
                Vertex3(position.DX + cx, position.DY + cy, position.DZ);
            }
        }

        public static void Rectangle(Rectangle rect) {
            Vector2 posForIndex(int i)
                => i switch {
                    0 => new Vector2(rect.Left, rect.Top),
                    1 => new Vector2(rect.Left, rect.Bottom),
                    2 => new Vector2(rect.Right, rect.Bottom),
                    3 => new Vector2(rect.Right, rect.Top)
                };
            for (int i = 0; i < 4; i++) {
                Vector2 pos = posForIndex(i);
                Vertex3(pos.X, pos.Y, 0.0f);
            }
            for (int i = 0; i < 4; i++) {
                Vector2 pos = posForIndex(3 - i);
                Vertex3(pos.X, pos.Y, 0.0f);
            }
        }

        public static void Line(Line line, float thickness = 1.0f, CBRE.DataStructures.Geometric.Matrix m = null) {
            var matrix = m ?? CBREMatrix.Identity;
            if (currentPrimitiveType.Value.IsLineType()) {
                Vertex3(line.Start * matrix);
                Vertex3(line.End * matrix);
            } else if (currentPrimitiveType is PrimitiveType.TriangleList) {
                var cylinderFaces = ShapeGenerator.Cylinder(
                    line,
                    radius: (decimal)thickness,
                    numSides: 8,
                    roundDecimals: 4);
                foreach (var face in cylinderFaces) {
                    for (int i = 2; i < face.Length; i++) {
                        Vertex3(face[i - 1] * matrix);
                        Vertex3(face[i] * matrix);
                        Vertex3(face[0] * matrix);
                    }
                }
            } else {
                throw new NotImplementedException($"{nameof(Line)} not implemented for {nameof(PrimitiveTopology)}.{currentPrimitiveType}");
            }
        }

        public static void FacesWireframe(
            IEnumerable<Face> faces, decimal thickness = 0.0m, CBRE.DataStructures.Geometric.Matrix m = null)
            => FacesWireframe(faces, thickness: (float)thickness, m: m);

        public static void FacesWireframe(IEnumerable<Face> faces, float thickness = 0.0f, CBRE.DataStructures.Geometric.Matrix m = null) {
            var matrix = m ?? CBRE.DataStructures.Geometric.Matrix.Identity;
            foreach (var face in faces) {
                foreach (var edge in face.GetEdges()) {
                    Line(edge, thickness, m);
                }
            }
        }

        public static void FacesSolid(IEnumerable<Face> faces, CBRE.DataStructures.Geometric.Matrix m = null) {
            var matrix = m ?? CBRE.DataStructures.Geometric.Matrix.Identity;
            foreach (var face in faces) {
                foreach (var tri in face.GetTriangles()) {
                    Vertex3(tri[0].Location * matrix);
                    Vertex3(tri[1].Location * matrix);
                    Vertex3(tri[2].Location * matrix);
                }
            }
        }

        public static void End() {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.End because a draw operation isn't in progress"); }
            GlobalGraphics.EndPass();

            int primCount = 0;
            PrimitiveTopology topology = PrimitiveTopology.PointList;

            switch (currentPrimitiveType) {
                case PrimitiveType.PointList:
                    topology = PrimitiveTopology.PointList;
                    primCount = vertices.Count;
                    break;
                case PrimitiveType.LineList:
                    topology = PrimitiveTopology.LineList;
                    primCount = vertices.Count / 2;
                    break;
                case PrimitiveType.LineLoop:
                    topology = PrimitiveTopology.LineStrip;
                    vertices.Add(vertices[0]);
                    primCount = vertices.Count - 1;
                    break;
                case PrimitiveType.LineStrip:
                    topology = PrimitiveTopology.LineStrip;
                    primCount = vertices.Count - 1;
                    break;
                case PrimitiveType.TriangleList:
                    topology = PrimitiveTopology.TriangleList;
                    primCount = vertices.Count / 3;
                    break;
                case PrimitiveType.TriangleStrip:
                    topology = PrimitiveTopology.TriangleStrip;
                    primCount = vertices.Count - 2;
                    break;
                /*case CustomPrimitiveTopology.TriangleFan:
                    primCount = vertices.Count - 2;
                    break;*/
                case PrimitiveType.QuadList:
                    var temp = new List<VertexPositionColorTexture>();
                    for (int i = 0; i < vertices.Count; i += 4) {
                        var v0 = vertices[i + 0];
                        var v1 = vertices[i + 1];
                        var v2 = vertices[i + 2];
                        var v3 = vertices[i + 3];
                        temp.Add(v0);
                        temp.Add(v1);
                        temp.Add(v2);

                        temp.Add(v0);
                        temp.Add(v2);
                        temp.Add(v3);
                    }

                    vertices.Clear();
                    vertices.AddRange(temp);

                    topology = PrimitiveTopology.TriangleList;
                    primCount = vertices.Count / 3;
                    break;
            }

            if (Texture == null) {
                Texture = GlobalGraphics.BlankWhiteTexture;
            }

            if (shaders == null) {

                string vertexCode = File.ReadAllText("Shaders/TexturedSolid.vert");
                string fragmentCode = File.ReadAllText("Shaders/TexturedSolid.frag");

                ShaderDescription vertexShaderDesc = new ShaderDescription(
                    ShaderStages.Vertex,
                    Encoding.UTF8.GetBytes(vertexCode),
                    "main");
                ShaderDescription fragmentShaderDesc = new ShaderDescription(
                    ShaderStages.Fragment,
                    Encoding.UTF8.GetBytes(fragmentCode),
                    "main");

                shaders = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, new CrossCompileOptions(GlobalGraphics.GraphicsDevice.BackendType == GraphicsBackend.OpenGL, GlobalGraphics.GraphicsDevice.BackendType == GraphicsBackend.Vulkan));
                shaderCompilerResult = SpirvCompilation.CompileVertexFragment(vertexShaderDesc.ShaderBytes, fragmentShaderDesc.ShaderBytes, CrossCompileTarget.GLSL);
            }

            if (vertices.Count > 0) {
                if (vertexBuffer == null || true) {
                    vertexBuffer?.Dispose();
                    vertexBuffer = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)vertices.Count * 4 * VertexPositionColorTexture.SizeInBytes, Veldrid.BufferUsage.VertexBuffer));
                }
                GlobalGraphics.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, vertices.ToArray());

                VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                    // shaderCompilerResult.Reflection.VertexElements
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Float3),
                    new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Float2),
                    new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Float4)
                );

                ResourceLayout textureLayout = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("MainTextureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )/*shaderCompilerResult.Reflection.ResourceLayouts[0]*/);
                {
                    var textureSet = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                        textureLayout, Texture, GlobalGraphics.PointSampler
                    ));
                    {

                        GraphicsPipelineDescription pipeDesc = new GraphicsPipelineDescription();
                        pipeDesc.BlendState = BlendStateDescription.SingleOverrideBlend;
                        pipeDesc.DepthStencilState = new DepthStencilStateDescription(
                            depthTestEnabled: true,
                            depthWriteEnabled: true,
                            comparisonKind: ComparisonKind.LessEqual
                        );
                        pipeDesc.DepthStencilState = DepthStencilStateDescription.Disabled;
                        pipeDesc.RasterizerState = new RasterizerStateDescription(
                            cullMode: FaceCullMode.Back,
                            fillMode: PolygonFillMode.Solid,
                            frontFace: FrontFace.CounterClockwise,
                            depthClipEnabled: true,
                            scissorTestEnabled: false
                        );
                        pipeDesc.RasterizerState = RasterizerStateDescription.CullNone;
                        pipeDesc.PrimitiveTopology = topology;
                        pipeDesc.ResourceLayouts = new[] { textureLayout };
                        pipeDesc.ShaderSet = new ShaderSetDescription(
                            vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                            shaders: shaders
                        );
                        pipeDesc.Outputs = GlobalGraphics.GraphicsDevice.SwapchainFramebuffer.OutputDescription;
                        var pipeline = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(pipeDesc);
                        {
                            GlobalGraphics.BeginPass();
                            GlobalGraphics.CommandList.SetPipeline(pipeline);
                            GlobalGraphics.CommandList.SetGraphicsResourceSet(0, textureSet);
                            GlobalGraphics.CommandList.SetVertexBuffer(0, vertexBuffer);
                            GlobalGraphics.CommandList.Draw((uint)vertices.Count);
                        }
                    }
                }
            }
            currentPrimitiveType = null;
            Texture = null;
        }
    }
}

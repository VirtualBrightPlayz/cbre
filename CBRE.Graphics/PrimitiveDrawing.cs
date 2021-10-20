using System;
using System.Collections.Generic;
using System.Text;
using CBRE.DataStructures.MapObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Graphics {
    public static class PrimitiveDrawing {
        private static PrimitiveType? currentPrimitiveType = null;

        private static Color color = Color.White;
        private static List<VertexPositionColorTexture> vertices = new List<VertexPositionColorTexture>();
        public static Texture2D Texture = null;

        public static void Begin(PrimitiveType primType) {
            if (currentPrimitiveType != null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Begin because a draw operation is already in progress"); }
            currentPrimitiveType = primType;
            vertices.Clear();
        }

        public static void SetColor(System.Drawing.Color clr) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Color4 because a draw operation isn't in progress"); }
            color.R = clr.R;
            color.G = clr.G;
            color.B = clr.B;
            color.A = clr.A;
        }

        public static void Vertex2(double x, double y, float u = 0f, float v = 0f) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Vertex3 because a draw operation isn't in progress"); }
            vertices.Add(new VertexPositionColorTexture() {
                Position = new Vector3((float)x, (float)y, 0.0f),
                Color = color,
                TextureCoordinate = new Vector2(u, v)
            });
        }

        public static void Vertex3(Vector3 position, float u = 0f, float v = 0f) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Vertex3 because a draw operation isn't in progress"); }
            vertices.Add(new VertexPositionColorTexture() {
                Position = position,
                Color = color,
                TextureCoordinate = new Vector2(u, v)
            });
        }

        public static void Vertex3(double x, double y, double z, float u = 0f, float v = 0f) {
            Vertex3(new Vector3((float)x, (float)y, (float)z), u, v);
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

        public static void Square(CBRE.DataStructures.Geometric.Vector3 position, double radius) {
            for (int i = 0; i < 4; i++) {
                double cx = Math.Cos(((double)i + 0.5f) * Math.PI * 2.0 / 4.0) * radius;
                double cy = Math.Sin(((double)i + 0.5f) * Math.PI * 2.0 / 4.0) * radius;
                Vertex3(position.DX + cx, position.DY + cy, position.DZ);
            }
        }

        public static void FacesWireframe(IEnumerable<Face> faces, CBRE.DataStructures.Geometric.Matrix m = null) {
            var matrix = m ?? CBRE.DataStructures.Geometric.Matrix.Identity;
            foreach (var face in faces) {
                foreach (var edge in face.GetEdges()) {
                    Vertex3(edge.Start * matrix);
                    Vertex3(edge.End * matrix);
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

            int primCount = 0;
            switch (currentPrimitiveType) {
                case PrimitiveType.PointList:
                    primCount = vertices.Count;
                    break;
                case PrimitiveType.LineList:
                    primCount = vertices.Count / 2;
                    break;
                case PrimitiveType.LineLoop:
                    primCount = vertices.Count;
                    break;
                case PrimitiveType.LineStrip:
                    primCount = vertices.Count - 1;
                    break;
                case PrimitiveType.TriangleList:
                    primCount = vertices.Count / 3;
                    break;
                case PrimitiveType.TriangleStrip:
                    primCount = vertices.Count - 2;
                    break;
                case PrimitiveType.TriangleFan:
                    primCount = vertices.Count - 2;
                    break;
                case PrimitiveType.QuadList:
                    primCount = vertices.Count / 4;
                    break;
            }

            if (vertices.Count > 0) {
                GlobalGraphics.GraphicsDevice.DrawUserPrimitives(
                    currentPrimitiveType.Value,
                    vertices.ToArray(),
                    0,
                    primCount);
            }
            currentPrimitiveType = null;
            Texture = null;
        }
    }
}

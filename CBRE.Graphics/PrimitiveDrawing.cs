using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Graphics {
    public static class PrimitiveDrawing {
        private static PrimitiveType? currentPrimitiveType = null;

        private static Color color = Color.White;
        private static List<VertexPositionColor> vertices = new List<VertexPositionColor>();

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

        public static void Vertex2(double x, double y) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Vertex3 because a draw operation isn't in progress"); }
            vertices.Add(new VertexPositionColor() {
                Position = new Vector3((float)x, (float)y, 0.0f),
                Color = color
            });
        }

        public static void Vertex3(Vector3 position) {
            if (currentPrimitiveType == null) { throw new InvalidOperationException("Cannot call PrimitiveDrawing.Vertex3 because a draw operation isn't in progress"); }
            vertices.Add(new VertexPositionColor() {
                Position = position,
                Color = color
            });
        }

        public static void Vertex3(double x, double y, double z) {
            Vertex3(new Vector3((float)x, (float)y, (float)z));
        }

        public static void Vertex3(CBRE.DataStructures.Geometric.Vector3 position) {
            Vertex3(position.DX, position.DY, position.DZ);
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

            GlobalGraphics.GraphicsDevice.DrawUserPrimitives(
                currentPrimitiveType.Value,
                vertices.ToArray(),
                0,
                primCount);
            currentPrimitiveType = null;
        }
    }
}

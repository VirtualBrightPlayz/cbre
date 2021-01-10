using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Graphics;
using CBRE.Providers.Texture;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Rendering {
    public class BrushRenderer {
        public BasicEffect BasicEffect;
        public Document Document;

        public struct BrushVertex : IVertexType {
            public Microsoft.Xna.Framework.Vector3 Position;
            public Microsoft.Xna.Framework.Vector3 Normal;
            public Microsoft.Xna.Framework.Vector2 DiffuseUV;
            public Microsoft.Xna.Framework.Vector2 LightmapUV;
            public Microsoft.Xna.Framework.Color Color;
            public static readonly VertexDeclaration VertexDeclaration;
            public BrushVertex(
                    Microsoft.Xna.Framework.Vector3 position,
                    Microsoft.Xna.Framework.Vector3 normal,
                    Microsoft.Xna.Framework.Vector2 diffUv,
                    Microsoft.Xna.Framework.Vector2 lmUv,
                    Microsoft.Xna.Framework.Color color
            ) {
                this.Position = position;
                this.Normal = normal;
                this.DiffuseUV = diffUv;
                this.LightmapUV = lmUv;
                this.Color = color;
            }

            VertexDeclaration IVertexType.VertexDeclaration {
                get {
                    return VertexDeclaration;
                }
            }

            static BrushVertex() {
                VertexElement[] elements = new VertexElement[] {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                    new VertexElement(40, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                };
                VertexDeclaration declaration = new VertexDeclaration(elements);
                VertexDeclaration = declaration;
            }
        };

        private class BrushGeometry : IDisposable {
            private BrushVertex[] vertices = null;
            private ushort[] indices3d = null;
            private ushort[] indices2d = null;
            private VertexBuffer vertexBuffer = null;
            private IndexBuffer index3dBuffer = null;
            private IndexBuffer index2dBuffer = null;
            private int vertexCount = 0;
            private int index3dCount = 0;
            private int index2dCount = 0;

            private List<Face> faces = new List<Face>();

            private bool dirty = false;

            private void UpdateBuffers() {
                if (!dirty) { return; }

                vertexCount = 0;
                index3dCount = 0;
                index2dCount = 0;
                for (int i=0;i<faces.Count;i++) {
                    faces[i].CalculateTextureCoordinates(true);
                    vertexCount += faces[i].Vertices.Count;
                    index3dCount += (faces[i].Vertices.Count - 2) * 3;
                    index2dCount += faces[i].Vertices.Count * 2;
                }

                if (vertices == null || vertices.Length < vertexCount) {
                    vertices = new BrushVertex[vertexCount * 2];
                    vertexBuffer?.Dispose();
                    vertexBuffer = new VertexBuffer(GlobalGraphics.GraphicsDevice, BrushVertex.VertexDeclaration, vertexCount * 2, BufferUsage.None);
                }

                if (indices3d == null || indices3d.Length < index3dCount) {
                    indices3d = new ushort[index3dCount * 2];
                    index3dBuffer?.Dispose();
                    index3dBuffer = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, index3dCount * 2, BufferUsage.None);
                }

                if (indices2d == null || indices2d.Length < index2dCount) {
                    indices2d = new ushort[index2dCount * 2];
                    index2dBuffer?.Dispose();
                    index2dBuffer = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, index2dCount * 2, BufferUsage.None);
                }

                int vertexIndex = 0;
                int index3dIndex = 0;
                int index2dIndex = 0;
                for (int i = 0; i < faces.Count; i++) {
                    for (int j=0;j<faces[i].Vertices.Count;j++) {
                        var location = faces[i].Vertices[j].Location;
                        vertices[vertexIndex + j].Position = new Microsoft.Xna.Framework.Vector3((float)location.X, (float)location.Y, (float)location.Z);
                        var normal = faces[i].Plane.Normal;
                        vertices[vertexIndex + j].Normal = new Microsoft.Xna.Framework.Vector3((float)normal.X, (float)normal.Y, (float)normal.Z);
                        var diffUv = new Microsoft.Xna.Framework.Vector2((float)faces[i].Vertices[j].DTextureU, (float)faces[i].Vertices[j].DTextureV);
                        vertices[vertexIndex + j].DiffuseUV = diffUv;
                        var lmUv = new Microsoft.Xna.Framework.Vector2((float)faces[i].Vertices[j].LMU, (float)faces[i].Vertices[j].LMV);
                        vertices[vertexIndex + j].LightmapUV = lmUv;
                        vertices[vertexIndex + j].Color = new Microsoft.Xna.Framework.Color(faces[i].Colour.R, faces[i].Colour.G, faces[i].Colour.B, faces[i].Colour.A);
                        indices2d[index2dIndex + (j * 2)] = (ushort)(vertexIndex + j);
                        indices2d[index2dIndex + (j * 2) + 1] = (ushort)(vertexIndex + ((j + 1) % faces[i].Vertices.Count));
                    }

                    index2dIndex += faces[i].Vertices.Count * 2;

                    foreach (var triangleIndex in faces[i].GetTriangleIndices()) {
                        indices3d[index3dIndex] = (ushort)(vertexIndex + triangleIndex);
                        index3dIndex++;
                    }
                    vertexIndex += faces[i].Vertices.Count;
                }

                vertexBuffer.SetData(vertices);
                index3dBuffer.SetData(indices3d);
                index2dBuffer.SetData(indices2d);

                dirty = false;
            }

            public void AddFace(Face face) {
                faces.Add(face);
                MarkDirty();
            }

            public void RemoveFace(Face face) {
                faces.Remove(face);
                MarkDirty();
            }

            public void MarkDirty() {
                dirty = true;
            }

            public bool HasFaces {
                get { return faces.Count > 0; }
            }

            public void RenderWireframe() {
                UpdateBuffers();
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = index2dBuffer;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, index2dCount / 2);
            }

            public void RenderSolid() {
                UpdateBuffers();
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = index3dBuffer;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, index3dCount / 3);
            }

            public void Dispose() {
                vertexBuffer?.Dispose();
                index3dBuffer?.Dispose();
            }
        }

        public void MarkDirty(string texName) {
            if (brushGeom.TryGetValue(texName, out BrushGeometry geom)) {
                geom.MarkDirty();
            }
        }

        private Dictionary<string, BrushGeometry> brushGeom = new Dictionary<string, BrushGeometry>();
        //private Dictionary<Face, ITexture> currentFaceTextures = new Dictionary<Face, ITexture>();

        public BrushRenderer(Document doc) {
            Document = doc;

            BasicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);

            foreach (Solid solid in doc.Map.WorldSpawn.Find(x => x is Solid).OfType<Solid>()) {
                solid.Faces.ForEach(f => AddFace(f));
            }
        }

        public void AddFace(Face face) {
            var textureName = face.Texture.Name;
            if (!brushGeom.ContainsKey(textureName)) {
                brushGeom.Add(textureName, new BrushGeometry());
            }
            brushGeom[textureName].AddFace(face);
        }

        public void RemoveFace(Face face) {
            var textureName = face.Texture.Name;
            if (!brushGeom.ContainsKey(textureName)) {
                return;
            }
            brushGeom[textureName].RemoveFace(face);
            if (!brushGeom[textureName].HasFaces) {
                brushGeom[textureName].Dispose();
                brushGeom.Remove(textureName);
            }
        }

        public Microsoft.Xna.Framework.Matrix World { get { return BasicEffect.World; } set { BasicEffect.World = value; } }
        public Microsoft.Xna.Framework.Matrix View { get { return BasicEffect.View; } set { BasicEffect.View = value; } }
        public Microsoft.Xna.Framework.Matrix Projection { get { return BasicEffect.Projection; } set { BasicEffect.Projection = value; } }

        public void RenderTextured() {
            foreach (var kvp in brushGeom) {
                TextureItem item = TextureProvider.GetItem(kvp.Key);
                if (item != null && item.Texture is AsyncTexture asyncTexture && asyncTexture.MonoGameTexture != null) {
                    BasicEffect.VertexColorEnabled = false;
                    BasicEffect.TextureEnabled = true;
                    BasicEffect.Texture = asyncTexture.MonoGameTexture;
                    BasicEffect.CurrentTechnique.Passes[0].Apply();
                    kvp.Value.RenderSolid();
                }
            }
        }

        public void RenderSolidUntextured() {
            foreach (var kvp in brushGeom) {
                BasicEffect.VertexColorEnabled = true;
                BasicEffect.TextureEnabled = false;
                BasicEffect.CurrentTechnique.Passes[0].Apply();
                kvp.Value.RenderSolid();
            }
        }

        public void RenderWireframe() {
            foreach (var kvp in brushGeom) {
                BasicEffect.VertexColorEnabled = true;
                BasicEffect.TextureEnabled = false;
                BasicEffect.CurrentTechnique.Passes[0].Apply();
                kvp.Value.RenderWireframe();
            }
        }
    }
}

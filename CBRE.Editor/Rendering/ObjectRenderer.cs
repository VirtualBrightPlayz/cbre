using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CBRE.Common;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Extensions;
using CBRE.FileSystem;
using CBRE.Graphics;
using CBRE.Providers.Model;
using CBRE.Providers.Texture;
using CBRE.Settings;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Editor.Rendering {
    public class ObjectRenderer {
        public BasicEffect BasicEffect;
        public Effect TexturedLightmapped;
        public Effect TexturedShaded;
        public Effect SolidShaded;
        public Effect Solid;
        public Document Document;

        public struct PointEntityVertex : IVertexType {
            public Microsoft.Xna.Framework.Vector3 Position;
            public Microsoft.Xna.Framework.Vector3 Normal;
            public Microsoft.Xna.Framework.Color Color;
            public static readonly VertexDeclaration VertexDeclaration;
            public PointEntityVertex(
                    Microsoft.Xna.Framework.Vector3 position,
                    Microsoft.Xna.Framework.Vector3 normal,
                    Microsoft.Xna.Framework.Color color
            ) {
                this.Position = position;
                this.Normal = normal;
                this.Color = color;
            }

            VertexDeclaration IVertexType.VertexDeclaration {
                get {
                    return VertexDeclaration;
                }
            }

            static PointEntityVertex() {
                VertexElement[] elements = new VertexElement[] {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                    new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                };
                VertexDeclaration declaration = new VertexDeclaration(elements);
                VertexDeclaration = declaration;
            }
        };

        private class PointEntityGeometry {
            public PointEntityGeometry(Document doc) { document = doc; }

            private Document document = null;
            private PointEntityVertex[] vertices = null;
            private ushort[] indicesSolid = null;
            private ushort[] indicesWireframe = null;
            private VertexBuffer vertexBuffer = null;
            private IndexBuffer indexBufferSolid = null;
            private IndexBuffer indexBufferWireframe = null;
            private int vertexCount = 0;
            private int indexSolidCount = 0;
            private int indexWireframeCount = 0;

            public void UpdateBuffers() {
                var entities = document.Map.WorldSpawn.Find(x => x is Entity e && e.GameData?.ClassType == DataStructures.GameData.ClassType.Point && !e.GameData.Behaviours.Any(p => p.Name == "sprite")).OfType<Entity>().ToList();
                vertexCount = entities.Count * 24;
                indexSolidCount = entities.Count * 36;
                indexWireframeCount = entities.Count * 24;

                if (entities.Count == 0) {
                    return;
                }

                if (vertexBuffer == null || vertices == null || vertices.Length < vertexCount) {
                    vertexBuffer = new VertexBuffer(GlobalGraphics.GraphicsDevice, PointEntityVertex.VertexDeclaration, vertexCount, BufferUsage.None);
                }

                if (indexBufferSolid == null || indicesSolid == null || indicesSolid.Length < indexSolidCount) {
                    indexBufferSolid = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, indexSolidCount, BufferUsage.None);
                }

                if (indexBufferWireframe == null || indexBufferWireframe == null || indicesWireframe.Length < indexWireframeCount) {
                    indexBufferWireframe = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, indexWireframeCount, BufferUsage.None);
                }

                vertices = new PointEntityVertex[vertexCount];
                indicesSolid = new ushort[indexSolidCount];
                indicesWireframe = new ushort[indexWireframeCount];

                void writeVertices(int i, Entity entity) {
                    int j = 0;
                    foreach (var face in entity.BoundingBox.GetBoxFaces()) {
                        var normal = (face[1] - face[0]).Cross(face[2] - face[0]).Normalise();
                        foreach (var point in face) {
                            vertices[(i * 24) + j].Position = new Microsoft.Xna.Framework.Vector3((float)point.X, (float)point.Y, (float)point.Z);
                            vertices[(i * 24) + j].Normal = new Microsoft.Xna.Framework.Vector3((float)normal.X, (float)normal.Y, (float)normal.Z);
                            vertices[(i * 24) + j].Color = new Microsoft.Xna.Framework.Color(entity.Colour.R, entity.Colour.G, entity.Colour.B, entity.Colour.A);
                            j++;
                        }
                    }
                }

                void writeIndicesSolid(int i) {
                    for (int j=0;j<6;j++) {
                        indicesSolid[(i * 36) + (j * 6) + 0] = (ushort)((i * 24) + (j * 4) + 0);
                        indicesSolid[(i * 36) + (j * 6) + 1] = (ushort)((i * 24) + (j * 4) + 1);
                        indicesSolid[(i * 36) + (j * 6) + 2] = (ushort)((i * 24) + (j * 4) + 2);
                        indicesSolid[(i * 36) + (j * 6) + 3] = (ushort)((i * 24) + (j * 4) + 0);
                        indicesSolid[(i * 36) + (j * 6) + 4] = (ushort)((i * 24) + (j * 4) + 2);
                        indicesSolid[(i * 36) + (j * 6) + 5] = (ushort)((i * 24) + (j * 4) + 3);
                    }
                }
                void writeIndicesWireframe(int i) {
                    //front
                    indicesWireframe[(i * 24) + 0] = (ushort)((i * 24) + 0);
                    indicesWireframe[(i * 24) + 1] = (ushort)((i * 24) + 1);
                    indicesWireframe[(i * 24) + 2] = (ushort)((i * 24) + 1);
                    indicesWireframe[(i * 24) + 3] = (ushort)((i * 24) + 2);
                    indicesWireframe[(i * 24) + 4] = (ushort)((i * 24) + 2);
                    indicesWireframe[(i * 24) + 5] = (ushort)((i * 24) + 3);
                    indicesWireframe[(i * 24) + 6] = (ushort)((i * 24) + 3);
                    indicesWireframe[(i * 24) + 7] = (ushort)((i * 24) + 0);

                    //back
                    indicesWireframe[(i * 24) + 8] = (ushort)((i * 24) + 4);
                    indicesWireframe[(i * 24) + 9] = (ushort)((i * 24) + 5);
                    indicesWireframe[(i * 24) + 10] = (ushort)((i * 24) + 5);
                    indicesWireframe[(i * 24) + 11] = (ushort)((i * 24) + 6);
                    indicesWireframe[(i * 24) + 12] = (ushort)((i * 24) + 6);
                    indicesWireframe[(i * 24) + 13] = (ushort)((i * 24) + 7);
                    indicesWireframe[(i * 24) + 14] = (ushort)((i * 24) + 7);
                    indicesWireframe[(i * 24) + 15] = (ushort)((i * 24) + 4);

                    //front to back
                    indicesWireframe[(i * 24) + 16] = (ushort)((i * 24) + 0);
                    indicesWireframe[(i * 24) + 17] = (ushort)((i * 24) + 5);
                    indicesWireframe[(i * 24) + 18] = (ushort)((i * 24) + 1);
                    indicesWireframe[(i * 24) + 19] = (ushort)((i * 24) + 4);
                    indicesWireframe[(i * 24) + 20] = (ushort)((i * 24) + 2);
                    indicesWireframe[(i * 24) + 21] = (ushort)((i * 24) + 7);
                    indicesWireframe[(i * 24) + 22] = (ushort)((i * 24) + 3);
                    indicesWireframe[(i * 24) + 23] = (ushort)((i * 24) + 6);
                }

                for (int i=0;i<entities.Count;i++) {
                    var entity = entities[i];
                    int j = 0;
                    writeVertices(i, entity);
                    writeIndicesSolid(i);
                    writeIndicesWireframe(i);
                }

                vertexBuffer.SetData(vertices);
                indexBufferSolid.SetData(indicesSolid);
                indexBufferWireframe.SetData(indicesWireframe);
            }

            public void RenderWireframe() {
                UpdateBuffers();
                if (indexWireframeCount == 0) {
                    return;
                }
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = indexBufferWireframe;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, indexWireframeCount / 2);
            }

            public void RenderSolid() {
                UpdateBuffers();
                if (indexSolidCount == 0) {
                    return;
                }
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = indexBufferSolid;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexSolidCount / 3);
            }
        };
        private PointEntityGeometry pointEntityGeometry;

        public struct BrushVertex : IVertexType {
            public Microsoft.Xna.Framework.Vector3 Position;
            public Microsoft.Xna.Framework.Vector3 Normal;
            public Microsoft.Xna.Framework.Vector2 DiffuseUV;
            public Microsoft.Xna.Framework.Vector2 LightmapUV;
            public Microsoft.Xna.Framework.Color Color;
            public float Selected;
            public static readonly VertexDeclaration VertexDeclaration;
            public BrushVertex(
                    Microsoft.Xna.Framework.Vector3 position,
                    Microsoft.Xna.Framework.Vector3 normal,
                    Microsoft.Xna.Framework.Vector2 diffUv,
                    Microsoft.Xna.Framework.Vector2 lmUv,
                    Microsoft.Xna.Framework.Color color,
                    bool selected
            ) {
                this.Position = position;
                this.Normal = normal;
                this.DiffuseUV = diffUv;
                this.LightmapUV = lmUv;
                this.Color = color;
                this.Selected = selected ? 1.0f : 0.0f;
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
                    new VertexElement(40, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                    new VertexElement(44, VertexElementFormat.Single, VertexElementUsage.Color, 1)
                };
                VertexDeclaration declaration = new VertexDeclaration(elements);
                VertexDeclaration = declaration;
            }
        };

        private class BrushGeometry : IDisposable {
            public BrushGeometry(Document doc) { document = doc; }

            private Document document;
            private BrushVertex[] vertices = null;
            private ushort[] indicesSolid = null;
            private ushort[] indicesWireframe = null;
            private VertexBuffer vertexBuffer = null;
            private IndexBuffer indexBufferSolid = null;
            private IndexBuffer indexBufferWireframe = null;
            private int vertexCount = 0;
            private int indexSolidCount = 0;
            private int indexWireframeCount = 0;

            private List<Face> faces = new List<Face>();

            private bool dirty = false;

            private void UpdateBuffers() {
                if (!dirty) { return; }

                vertexCount = 0;
                indexSolidCount = 0;
                indexWireframeCount = 0;
                for (int i=0;i<faces.Count;i++) {
                    faces[i].CalculateTextureCoordinates(true);
                    vertexCount += faces[i].Vertices.Count;
                    indexSolidCount += (faces[i].Vertices.Count - 2) * 3;
                    indexWireframeCount += faces[i].Vertices.Count * 2;
                }

                if (vertices == null || vertices.Length < vertexCount) {
                    vertices = new BrushVertex[vertexCount * 2];
                    vertexBuffer?.Dispose();
                    vertexBuffer = new VertexBuffer(GlobalGraphics.GraphicsDevice, BrushVertex.VertexDeclaration, vertexCount * 2, BufferUsage.None);
                }

                if (indicesSolid == null || indicesSolid.Length < indexSolidCount) {
                    indicesSolid = new ushort[indexSolidCount * 2];
                    indexBufferSolid?.Dispose();
                    indexBufferSolid = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, indexSolidCount * 2, BufferUsage.None);
                }

                if (indicesWireframe == null || indicesWireframe.Length < indexWireframeCount) {
                    indicesWireframe = new ushort[indexWireframeCount * 2];
                    indexBufferWireframe?.Dispose();
                    indexBufferWireframe = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, indexWireframeCount * 2, BufferUsage.None);
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
                        vertices[vertexIndex + j].Selected = document.Selection.IsFaceSelected(faces[i]) ? 1.0f : 0.0f;
                        indicesWireframe[index2dIndex + (j * 2)] = (ushort)(vertexIndex + j);
                        indicesWireframe[index2dIndex + (j * 2) + 1] = (ushort)(vertexIndex + ((j + 1) % faces[i].Vertices.Count));
                    }

                    index2dIndex += faces[i].Vertices.Count * 2;

                    foreach (var triangleIndex in faces[i].GetTriangleIndices()) {
                        indicesSolid[index3dIndex] = (ushort)(vertexIndex + triangleIndex);
                        index3dIndex++;
                    }
                    vertexIndex += faces[i].Vertices.Count;
                }

                vertexBuffer.SetData(vertices);
                indexBufferSolid.SetData(indicesSolid);
                indexBufferWireframe.SetData(indicesWireframe);

                dirty = false;
            }

            public void AddFace(Face face) {
                if (!faces.Contains(face)) { faces.Add(face); }
                MarkDirty();
            }

            public void RemoveFace(Face face) {
                if (!faces.Contains(face)) { return; }

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
                if (indexSolidCount <= 0)
                    return;
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = indexBufferWireframe;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, indexWireframeCount / 2);
            }

            public void RenderSolid() {
                UpdateBuffers();
                if (indexSolidCount <= 0)
                    return;
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = indexBufferSolid;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexSolidCount / 3);
            }

            public void ResetLightmapped() {
                vertexCount = 0;
                indexSolidCount = 0;
                indexWireframeCount = 0;
                for (int i=0;i<faces.Count;i++) {
                    faces[i].CalculateTextureCoordinates(true);
                    vertexCount += faces[i].Vertices.Count;
                    indexSolidCount += (faces[i].Vertices.Count - 2) * 3;
                    indexWireframeCount += faces[i].Vertices.Count * 2;
                }

                if (vertices == null || vertices.Length < vertexCount) {
                    vertices = new BrushVertex[vertexCount * 2];
                    vertexBuffer?.Dispose();
                    vertexBuffer = new VertexBuffer(GlobalGraphics.GraphicsDevice, BrushVertex.VertexDeclaration, vertexCount * 2, BufferUsage.None);
                }

                if (indicesSolid == null || indicesSolid.Length < indexSolidCount) {
                    indicesSolid = new ushort[indexSolidCount * 2];
                    indexBufferSolid?.Dispose();
                    indexBufferSolid = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, indexSolidCount * 2, BufferUsage.None);
                }

                if (indicesWireframe == null || indicesWireframe.Length < indexWireframeCount) {
                    indicesWireframe = new ushort[indexWireframeCount * 2];
                    indexBufferWireframe?.Dispose();
                    indexBufferWireframe = new IndexBuffer(GlobalGraphics.GraphicsDevice, IndexElementSize.SixteenBits, indexWireframeCount * 2, BufferUsage.None);
                }
            }

            public void RenderLightmapped(int id, bool last) {
                // if (!dirty) { return; }
                // UpdateLightmapBuffers(id);
                if (indexSolidCount <= 0)
                    return;
                GlobalGraphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
                GlobalGraphics.GraphicsDevice.Indices = indexBufferSolid;
                GlobalGraphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexSolidCount / 3);
                // dirty |= last;
            }

            public void UpdateLightmapBuffers(int id) {
                if (!dirty) return;
                dirty = false;
                var filteredFaces = faces.ToList();

                int vertexIndex = 0;
                int index3dIndex = 0;
                int index2dIndex = 0;
                for (int i = 0; i < filteredFaces.Count; i++) {
                    // if (filteredFaces[i].LmIndex != id) continue;
                    for (int j=0;j<filteredFaces[i].Vertices.Count;j++) {
                        var location = faces[i].Vertices[j].Location;
                        vertices[vertexIndex + j].Position = new Microsoft.Xna.Framework.Vector3((float)location.X, (float)location.Y, (float)location.Z);
                        var normal = filteredFaces[i].Plane.Normal;
                        vertices[vertexIndex + j].Normal = new Microsoft.Xna.Framework.Vector3((float)normal.X, (float)normal.Y, (float)normal.Z);
                        var diffUv = new Microsoft.Xna.Framework.Vector2((float)filteredFaces[i].Vertices[j].DTextureU, (float)filteredFaces[i].Vertices[j].DTextureV);
                        vertices[vertexIndex + j].DiffuseUV = diffUv;
                        var lmUv = new Microsoft.Xna.Framework.Vector2((float)filteredFaces[i].Vertices[j].LMU, (float)filteredFaces[i].Vertices[j].LMV);
                        vertices[vertexIndex + j].LightmapUV = lmUv;
                        vertices[vertexIndex + j].Color = new Microsoft.Xna.Framework.Color(filteredFaces[i].Colour.R, filteredFaces[i].Colour.G, filteredFaces[i].Colour.B, filteredFaces[i].Colour.A);
                        vertices[vertexIndex + j].Selected = document.Selection.IsFaceSelected(filteredFaces[i]) ? 1.0f : 0.0f;
                        indicesWireframe[index2dIndex + (j * 2)] = (ushort)(vertexIndex + j);
                        indicesWireframe[index2dIndex + (j * 2) + 1] = (ushort)(vertexIndex + ((j + 1) % filteredFaces[i].Vertices.Count));
                    }

                    index2dIndex += filteredFaces[i].Vertices.Count * 2;

                    foreach (var triangleIndex in filteredFaces[i].GetTriangleIndices()) {
                        indicesSolid[index3dIndex] = (ushort)(vertexIndex + triangleIndex);
                        index3dIndex++;
                    }
                    vertexIndex += filteredFaces[i].Vertices.Count;
                }

                vertexBuffer.SetData(vertices);
                indexBufferSolid.SetData(indicesSolid);
                indexBufferWireframe.SetData(indicesWireframe);
            }

            public void Dispose() {
                vertexBuffer?.Dispose();
                indexBufferSolid?.Dispose();
            }
        }


        public void MarkDirty() {
            foreach (var kvp in brushGeom) {
                kvp.Value.MarkDirty();
            }
        }

        public void MarkDirty(string texName) {
            if (brushGeom.TryGetValue(texName.ToLowerInvariant(), out BrushGeometry geom)) {
                geom.MarkDirty();
            }
        }

        public void AddFaces(MapObject mapObject) {
            switch (mapObject)
            {
                case Solid s:
                    foreach (var f in s.Faces) { AddFace(f); }
                    break;
                case Group g:
                    foreach (var child in g.GetChildren()) { AddFaces(child); }
                    break;
            }
        }
        
        public void RemoveFaces(MapObject mapObject) {
            switch (mapObject)
            {
                case Solid s:
                    foreach (var f in s.Faces) { RemoveFace(f); }
                    break;
                case Group g:
                    foreach (var child in g.GetChildren()) { RemoveFaces(child); }
                    break;
            }
        }
        
        public void MarkDirty(MapObject mapObject) {
            switch (mapObject)
            {
                case Solid s:
                    foreach (var t in s.Faces.Select(f => f.Texture.Name).Distinct()) { MarkDirty(t); }
                    break;
                case Group g:
                    foreach (var child in g.GetChildren()) { MarkDirty(child); }
                    break;
            }
        }

        private Dictionary<string, BrushGeometry> brushGeom = new Dictionary<string, BrushGeometry>();

        private Effect LoadEffect(string filename) {
            using (var fs = File.OpenRead(filename)) {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                return new Effect(GlobalGraphics.GraphicsDevice, bytes);
            }
        }

        public ObjectRenderer(Document doc) {
            Document = doc;

            BasicEffect = new BasicEffect(GlobalGraphics.GraphicsDevice);
            TexturedLightmapped = LoadEffect("Shaders/texturedLightmapped.mgfx");
            TexturedShaded = LoadEffect("Shaders/texturedShaded.mgfx");
            SolidShaded = LoadEffect("Shaders/solidShaded.mgfx");
            Solid = LoadEffect("Shaders/solid.mgfx");

            foreach (Solid solid in doc.Map.WorldSpawn.Find(x => x is Solid).OfType<Solid>()) {
                solid.Faces.ForEach(f => AddFace(f));
            }
            pointEntityGeometry = new PointEntityGeometry(doc);
        }

        public void AddFace(Face face) {
            var textureName = face.Texture.Name.ToLowerInvariant();
            if (!brushGeom.ContainsKey(textureName)) {
                brushGeom.Add(textureName, new BrushGeometry(Document));
            }
            brushGeom[textureName].AddFace(face);
        }

        public void RemoveFace(Face face) {
            var textureName = face.Texture.Name.ToLowerInvariant();
            if (!brushGeom.ContainsKey(textureName)) {
                return;
            }
            brushGeom[textureName].RemoveFace(face);
            if (!brushGeom[textureName].HasFaces) {
                brushGeom[textureName].Dispose();
                brushGeom.Remove(textureName);
            }
        }

        public Microsoft.Xna.Framework.Matrix World {
            get { return BasicEffect.World; }
            set {
                BasicEffect.World = value;
                TexturedLightmapped.Parameters["World"].SetValue(value);
                TexturedShaded.Parameters["World"].SetValue(value);
                SolidShaded.Parameters["World"].SetValue(value);
                Solid.Parameters["World"].SetValue(value);
            }
        }

        public Microsoft.Xna.Framework.Matrix View {
            get { return BasicEffect.View; }
            set {
                BasicEffect.View = value;
                TexturedLightmapped.Parameters["View"].SetValue(value);
                TexturedShaded.Parameters["View"].SetValue(value);
                SolidShaded.Parameters["View"].SetValue(value);
                Solid.Parameters["View"].SetValue(value);
            }
        }

        public Microsoft.Xna.Framework.Matrix Projection {
            get { return BasicEffect.Projection; }
            set {
                BasicEffect.Projection = value;
                TexturedLightmapped.Parameters["Projection"].SetValue(value);
                TexturedShaded.Parameters["Projection"].SetValue(value);
                SolidShaded.Parameters["Projection"].SetValue(value);
                Solid.Parameters["Projection"].SetValue(value);
            }
        }

        private readonly List<(AsyncTexture Texture, BrushGeometry Geometry)> translucentGeom = new();

        private Dictionary<string, ModelReference> models = new Dictionary<string, ModelReference>();

        public void RenderTextured() {
            translucentGeom.Clear();
            foreach (var kvp in brushGeom) {
                TextureItem item = TextureProvider.GetItem(kvp.Key);
                if (item is {Texture: AsyncTexture {MonoGameTexture: { }} asyncTexture}) {
                    if (asyncTexture.HasTransparency()) {
                        translucentGeom.Add((asyncTexture, kvp.Value));
                        continue;
                    } else {
                        TexturedShaded.Parameters["xTexture"].SetValue(asyncTexture.MonoGameTexture);
                        TexturedShaded.CurrentTechnique.Passes[0].Apply();
                    }
                } else {
                    SolidShaded.CurrentTechnique.Passes[0].Apply();
                }
                kvp.Value.RenderSolid();
            }
            
            SolidShaded.CurrentTechnique.Passes[0].Apply();
            pointEntityGeometry.RenderSolid();

            var prevDepthStencilState = GlobalGraphics.GraphicsDevice.DepthStencilState;
            GlobalGraphics.GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true, DepthBufferWriteEnable = false };
            foreach (var (texture, geometry) in translucentGeom) {
                TexturedShaded.Parameters["xTexture"].SetValue(texture.MonoGameTexture);
                TexturedShaded.CurrentTechnique.Passes[0].Apply();
                geometry.RenderSolid();
            }
            GlobalGraphics.GraphicsDevice.DepthStencilState = prevDepthStencilState;
        }

        public void RenderSprites(Viewport3D vp) {
            BasicEffect.CurrentTechnique.Passes[0].Apply();
            var sprites = Document.Map.WorldSpawn.Find(x => x is Entity e && e.GameData != null && e.GameData.Behaviours.Any(p => p.Name == "sprite")).OfType<Entity>().ToList();
            foreach (var sprite in sprites) {
                string key = sprite.GameData.Behaviours.FirstOrDefault(p => p.Name == "sprite").Values.FirstOrDefault();
                string color = sprite.GameData.Behaviours.FirstOrDefault(p => p.Name == "spritecolor").Values.FirstOrDefault();
                Property prop = sprite.EntityData.Properties.FirstOrDefault(p => p.Key == color);
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                TextureItem tex = TextureProvider.GetItem(key);
                if (tex != null && tex.Texture is AsyncTexture t) {
                    PrimitiveDrawing.Begin(PrimitiveType.QuadList);
                    var c = sprite.Origin;
                    var fcolor = prop == null ? Vector3.One * 255f : prop.GetVector3(Vector3.One * 255f);
                    t.Bind();
                    double amount = 25.0;
                    var up = vp.Camera.GetUp().Normalise() * amount;
                    var right = vp.Camera.GetRight().Normalise() * amount;
                    PrimitiveDrawing.Vertex3(c + up - right, 0f, 0f);
                    PrimitiveDrawing.Vertex3(c + up + right, 1f, 0f);
                    PrimitiveDrawing.Vertex3(c - up + right, 1f, 1f);
                    PrimitiveDrawing.Vertex3(c - up - right, 0f, 1f);
                    BasicEffect.Texture = PrimitiveDrawing.Texture;
                    BasicEffect.DiffuseColor = fcolor.ToXna() / 255f;
                    BasicEffect.TextureEnabled = true;
                    BasicEffect.VertexColorEnabled = false;
                    BasicEffect.CurrentTechnique.Passes[0].Apply();
                    PrimitiveDrawing.End();
                    t.Unbind();
                }
            }
            BasicEffect.TextureEnabled = false;
            BasicEffect.VertexColorEnabled = true;
        }

        public void RenderModels() {
            // Models
            BasicEffect.CurrentTechnique.Passes[0].Apply();
            var models = Document.Map.WorldSpawn
                .Find(x => x is Entity e && e.GameData != null && e.GameData.Behaviours.Any(p => p.Name == "model"))
                .OfType<Entity>().ToList();
            foreach (var model in models) {
                string key = model.GameData.Behaviours.FirstOrDefault(p => p.Name == "model").Values.FirstOrDefault();
                string path = Directories.GetModelPath(model.EntityData.GetPropertyValue(key));
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                NativeFile file = new NativeFile(path);
                if (this.models.ContainsKey(path)) {
                    Vector3 euler = model.EntityData.GetPropertyVector3("angles", Vector3.Zero);
                    Vector3 scale = model.EntityData.GetPropertyVector3("scale", Vector3.One);
                    Matrix modelMat = Matrix.Translation(model.Origin)
                                      * Matrix.RotationX(DMath.DegreesToRadians(euler.X))
                                      * Matrix.RotationY(DMath.DegreesToRadians(euler.Z))
                                      * Matrix.RotationZ(DMath.DegreesToRadians(euler.Y))
                                      * Matrix.Scale(scale);
                    ModelRenderer.Render(this.models[path].Model, modelMat, BasicEffect);
                } else if (ModelProvider.CanLoad(file)) {
                    ModelReference mref = ModelProvider.CreateModelReference(file);
                    this.models.Add(path, mref);
                }
            }
        }

        public void RenderLightmapped() {
            TextureItem rem = TextureProvider.GetItem("tooltextures/remove_face");
            TexturedLightmapped.Parameters["Alpha"].SetValue(1f/*Document.MGLightmaps.Length*/);
            foreach (var kvp in brushGeom) {
                kvp.Value.ResetLightmapped();
                for (int i = 0; i < Document.MGLightmaps.Length; i++) {
                    TextureItem item = TextureProvider.GetItem(kvp.Key);
                    
                    if (Document.MGLightmaps[i] != null) {
                        TexturedLightmapped.Parameters["yTexture"].SetValue(Document.MGLightmaps[i]);
                    }
                    else {
                        // Logging.Logger.Log(new Logging.ExceptionInfo(new Exception("No lightmap texture."), ""));
                    }

                    if (item != null && item.Texture is AsyncTexture asyncTexture && asyncTexture.MonoGameTexture != null) {
                        TexturedLightmapped.Parameters["xTexture"].SetValue(asyncTexture.MonoGameTexture);
                    }
                    TexturedLightmapped.CurrentTechnique.Passes[0].Apply();
                    kvp.Value.UpdateLightmapBuffers(i);
                }
                {
                    int i = 0;
                    kvp.Value.RenderLightmapped(i, i + 1 >= Document.MGLightmaps.Length);
                }
            }
            SolidShaded.CurrentTechnique.Passes[0].Apply();
            pointEntityGeometry.RenderSolid();

        }

        public void RenderSolidUntextured() {
            foreach (var kvp in brushGeom) {
                SolidShaded.CurrentTechnique.Passes[0].Apply();
                kvp.Value.RenderSolid();
            }
            SolidShaded.CurrentTechnique.Passes[0].Apply();
            pointEntityGeometry.RenderSolid();
        }

        public void RenderFlatUntextured() {
            foreach (var kvp in brushGeom) {
                Solid.CurrentTechnique.Passes[0].Apply();
                kvp.Value.RenderSolid();
            }
            SolidShaded.CurrentTechnique.Passes[0].Apply();
            pointEntityGeometry.RenderSolid();
        }

        public void RenderWireframe() {
            Solid.CurrentTechnique.Passes[0].Apply();
            foreach (var kvp in brushGeom) {
                kvp.Value.RenderWireframe();
            }
            pointEntityGeometry.RenderWireframe();
        }
    }
}

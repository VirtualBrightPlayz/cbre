using System.Collections.Immutable;
using System.Drawing;
using CBRE.DataStructures.Geometric;

namespace CBRE.RMesh;

public partial record RMesh {
    public static class Loader {
        public static RMesh FromFile(string filePath) =>
            FromStream(new FileStream(filePath, FileMode.Open));

        public static RMesh FromStream(Stream stream) {
            using BlitzReader reader = new BlitzReader(stream);

            string header = reader.ReadString();
            if (!HeaderUtils.IsHeaderValid(header, out HeaderUtils.HeaderSuffix headerSuffixes)) {
                throw new Exception($"Invalid header: {header}");
            }

            ImmutableArray<VisibleMesh> visibleMeshes = ReadVisibleMeshes(reader);
            ImmutableArray<InvisibleCollisionMesh>
                invisibleCollisionMeshes = ReadInvisibleCollisionMeshes(reader);

            ImmutableArray<VisibleMesh>? visibleNoCollMeshes = null;
            if (headerSuffixes.HasFlag(HeaderUtils.HeaderSuffix.HasNoColl)) {
                visibleNoCollMeshes = ReadVisibleMeshes(reader);
            }

            ImmutableArray<TriggerBox>? triggerBoxes = null;
            if (headerSuffixes.HasFlag(HeaderUtils.HeaderSuffix.HasTriggerBox)) {
                triggerBoxes = ReadTriggerBoxes(reader);
            }

            ReadEntities(reader);

            return new RMesh(
                visibleMeshes,
                invisibleCollisionMeshes,
                visibleNoCollMeshes,
                triggerBoxes,
                null);
        }

        private static ImmutableArray<VisibleMesh> ReadVisibleMeshes(BlitzReader reader) {
            int meshCount = reader.ReadInt();

            VisibleMesh[] meshes = new VisibleMesh[meshCount];
            foreach (int meshIndex in Enumerable.Range(0, meshCount)) {
                void readTextureInfo(out string? textureName, out bool textureIsTranslucent) {
                    int textureType = reader.ReadByte();

                    bool layerHasTexture = textureType != 0;
                    textureIsTranslucent = textureType == 3;

                    textureName = layerHasTexture ? reader.ReadString() : null;
                }

                readTextureInfo(out string? texture0, out bool translucent0);
                readTextureInfo(out string? texture1, out bool translucent1);

                string diffuseTextureName;
                bool isDiffuseTranslucent;
                string? lightmapTextureName;
                if (string.IsNullOrEmpty(texture1)) {
                    diffuseTextureName = texture0!;
                    isDiffuseTranslucent = translucent0;
                    lightmapTextureName = null;
                } else {
                    //Lightmap texture might be defined as the empty string because of
                    //a bug in the old RMesh converter, so let's account for that here
                    lightmapTextureName = texture0 == "" ? null : texture0;

                    diffuseTextureName = texture1;
                    isDiffuseTranslucent = translucent1;
                }

                VisibleMesh.BlendMode blendMode = isDiffuseTranslucent
                    ? VisibleMesh.BlendMode.Translucent
                    : lightmapTextureName is null
                        ? VisibleMesh.BlendMode.Opaque
                        : VisibleMesh.BlendMode.Lightmapped;

                int vertexCount = reader.ReadInt();
                VisibleMesh.Vertex[] vertices = new VisibleMesh.Vertex[vertexCount];
                foreach (int vertexIndex in Enumerable.Range(0, vertexCount)) {
                    float x = reader.ReadFloat();
                    float y = reader.ReadFloat();
                    float z = reader.ReadFloat();

                    float diffU = reader.ReadFloat();
                    float diffV = reader.ReadFloat();
                    float lmU = reader.ReadFloat();
                    float lmV = reader.ReadFloat();

                    byte r = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte b = reader.ReadByte();

                    vertices[vertexIndex] = new VisibleMesh.Vertex(
                        Position: new Vector3F(x, y, z),
                        DiffuseUv: new Vector2F(diffU, diffV),
                        LightmapUv: new Vector2F(lmU, lmV),
                        Color: Color.FromArgb(0xff, r, g, b));
                }

                int triangleCount = reader.ReadInt();
                Triangle[] triangles = new Triangle[triangleCount];
                foreach (int triangleIndex in Enumerable.Range(0, triangleCount)) {
                    ushort index0 = (ushort)reader.ReadInt();
                    ushort index1 = (ushort)reader.ReadInt();
                    ushort index2 = (ushort)reader.ReadInt();

                    triangles[triangleIndex] = new Triangle(index0, index1, index2);
                }

                meshes[meshIndex]
                    = new VisibleMesh(
                        vertices.ToImmutableArray(),
                        triangles.ToImmutableArray(),
                        diffuseTextureName,
                        lightmapTextureName,
                        blendMode);
            }

            return meshes.ToImmutableArray();
        }

        private static ImmutableArray<InvisibleCollisionMesh> ReadInvisibleCollisionMeshes(BlitzReader reader) {
            int meshCount = reader.ReadInt();

            InvisibleCollisionMesh[] meshes = new InvisibleCollisionMesh[meshCount];
            foreach (int meshIndex in Enumerable.Range(0, meshCount)) {
                int vertexCount = reader.ReadInt();
                InvisibleCollisionMesh.Vertex[] vertices = new InvisibleCollisionMesh.Vertex[vertexCount];
                foreach (int vertexIndex in Enumerable.Range(0, vertexCount)) {
                    float x = reader.ReadFloat();
                    float y = reader.ReadFloat();
                    float z = reader.ReadFloat();

                    vertices[vertexIndex] = new InvisibleCollisionMesh.Vertex(
                        Position: new Vector3F(x, y, z));
                }

                int triangleCount = reader.ReadInt();
                Triangle[] triangles = new Triangle[triangleCount];
                foreach (int triangleIndex in Enumerable.Range(0, triangleCount)) {
                    ushort index0 = (ushort)reader.ReadInt();
                    ushort index1 = (ushort)reader.ReadInt();
                    ushort index2 = (ushort)reader.ReadInt();

                    triangles[triangleIndex] = new Triangle(index0, index1, index2);
                }

                meshes[meshIndex]
                    = new InvisibleCollisionMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray());
            }

            return meshes.ToImmutableArray();
        }

        private static ImmutableArray<TriggerBox> ReadTriggerBoxes(BlitzReader reader) {
            int triggerBoxCount = reader.ReadInt();
            TriggerBox[] triggerBoxes = new TriggerBox[triggerBoxCount];
            foreach (int triggerBoxIndex in Enumerable.Range(0, triggerBoxCount)) {
                ImmutableArray<InvisibleCollisionMesh> submeshes = ReadInvisibleCollisionMeshes(reader);
                string name = reader.ReadString();
                triggerBoxes[triggerBoxIndex] = new TriggerBox(name, submeshes);
            }

            return triggerBoxes.ToImmutableArray();
        }

        private static void ReadEntities(BlitzReader reader) {
            int entityCount = reader.ReadInt();
            //TODO: implement
        }
    }
}

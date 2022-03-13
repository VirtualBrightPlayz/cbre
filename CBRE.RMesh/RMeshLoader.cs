using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using CBRE.DataStructures.Geometric;

namespace CBRE.RMesh; 

public static class RMeshLoader {
    private const string HeaderBase = "RoomMesh";

    [Flags]
    public enum HeaderSuffix {
        None = 0x0,
        HasTriggerBox = 0x1,
        HasNoColl = 0x2
    }

    private static bool IsHeaderValid(string header, out HeaderSuffix headerSuffixes) {
        string[] split = header.Split('.');
        headerSuffixes = HeaderSuffix.None;
        var possibleSuffixes = Enum.GetValues<HeaderSuffix>()
            .Where(s => s != HeaderSuffix.None)
            .ToImmutableHashSet();
        foreach (HeaderSuffix suffix in possibleSuffixes) {
            if (split.Contains(suffix.ToString())) { headerSuffixes |= suffix; }
        }

        return split[0] == HeaderBase
               && split.Skip(1).All(possibleSuffixes.Select(s => s.ToString()).Contains);
    }

    public static RMesh FromFile(string filePath) {
        using BlitzReader reader = new BlitzReader(filePath);

        string header = reader.ReadString();
        if (!IsHeaderValid(header, out HeaderSuffix headerSuffixes)) { throw new Exception($"Invalid header: {header}"); }

        ImmutableArray<RMesh.VisibleMesh> visibleMeshes = ReadVisibleMeshes(reader);
        ImmutableArray<RMesh.InvisibleCollisionMesh> invisibleCollisionMeshes = ReadInvisibleCollisionMeshes(reader);

        ImmutableArray<RMesh.VisibleMesh>? visibleNoCollMeshes = null;
        if (headerSuffixes.HasFlag(HeaderSuffix.HasNoColl)) {
            visibleNoCollMeshes = ReadVisibleMeshes(reader);
        }

        ImmutableArray<RMesh.TriggerBox>? triggerBoxes = null;
        if (headerSuffixes.HasFlag(HeaderSuffix.HasTriggerBox)) {
            triggerBoxes = ReadTriggerBoxes(reader);
        }
        
        return new RMesh(
            header,
            visibleMeshes,
            invisibleCollisionMeshes,
            visibleNoCollMeshes,
            triggerBoxes);
    }

    private static ImmutableArray<RMesh.VisibleMesh> ReadVisibleMeshes(BlitzReader reader) {
        int meshCount = reader.ReadInt();

        RMesh.VisibleMesh[] meshes = new RMesh.VisibleMesh[meshCount];
        foreach (int meshIndex in Enumerable.Range(0, meshCount)) {
            void readTextureInfo(out string? textureName, out bool textureIsTranslucent) {
                int textureType = reader.ReadByte();
                
                bool layerHasTexture = textureType != 0;
                textureIsTranslucent = textureType == 3;

                textureName = layerHasTexture ? reader.ReadString() : null;
            }
            
            readTextureInfo(out string? texture0, out bool translucent0);
            readTextureInfo(out string? texture1, out bool translucent1);

            string diffuseTextureName; bool isDiffuseTranslucent;
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

            RMesh.VisibleMesh.BlendMode blendMode = isDiffuseTranslucent
                ? RMesh.VisibleMesh.BlendMode.Translucent
                : lightmapTextureName is null
                    ? RMesh.VisibleMesh.BlendMode.Opaque
                    : RMesh.VisibleMesh.BlendMode.Lightmapped;

            int vertexCount = reader.ReadInt();
            RMesh.VisibleMesh.Vertex[] vertices = new RMesh.VisibleMesh.Vertex[vertexCount];
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

                vertices[vertexIndex] = new RMesh.VisibleMesh.Vertex(
                    Position: new Vector3F(x, y, z),
                    DiffuseUv: new Vector2F(diffU, diffV),
                    LightmapUv: new Vector2F(lmU, lmV),
                    Color: Color.FromArgb(0xff, r, g, b));
            }

            int triangleCount = reader.ReadInt();
            RMesh.Triangle[] triangles = new RMesh.Triangle[triangleCount];
            foreach (int triangleIndex in Enumerable.Range(0, triangleCount)) {
                ushort index0 = (ushort)reader.ReadInt();
                ushort index1 = (ushort)reader.ReadInt();
                ushort index2 = (ushort)reader.ReadInt();

                triangles[triangleIndex] = new RMesh.Triangle(index0, index1, index2);
            }
            
            meshes[meshIndex]
                = new RMesh.VisibleMesh(
                    vertices.ToImmutableArray(),
                    triangles.ToImmutableArray(),
                    diffuseTextureName,
                    lightmapTextureName,
                    blendMode);
        }

        return meshes.ToImmutableArray();
    }
    
    private static ImmutableArray<RMesh.InvisibleCollisionMesh> ReadInvisibleCollisionMeshes(BlitzReader reader) {
        int meshCount = reader.ReadInt();

        RMesh.InvisibleCollisionMesh[] meshes = new RMesh.InvisibleCollisionMesh[meshCount];
        foreach (int meshIndex in Enumerable.Range(0, meshCount)) {
            int vertexCount = reader.ReadInt();
            RMesh.InvisibleCollisionMesh.Vertex[] vertices = new RMesh.InvisibleCollisionMesh.Vertex[vertexCount];
            foreach (int vertexIndex in Enumerable.Range(0, vertexCount)) {
                float x = reader.ReadFloat();
                float y = reader.ReadFloat();
                float z = reader.ReadFloat();

                vertices[vertexIndex] = new RMesh.InvisibleCollisionMesh.Vertex(
                    Position: new Vector3F(x, y, z));
            }

            int triangleCount = reader.ReadInt();
            RMesh.Triangle[] triangles = new RMesh.Triangle[triangleCount];
            foreach (int triangleIndex in Enumerable.Range(0, triangleCount)) {
                ushort index0 = (ushort)reader.ReadInt();
                ushort index1 = (ushort)reader.ReadInt();
                ushort index2 = (ushort)reader.ReadInt();

                triangles[triangleIndex] = new RMesh.Triangle(index0, index1, index2);
            }

            meshes[meshIndex]
                = new RMesh.InvisibleCollisionMesh(vertices.ToImmutableArray(), triangles.ToImmutableArray());
        }

        return meshes.ToImmutableArray();
    }

    private static ImmutableArray<RMesh.TriggerBox> ReadTriggerBoxes(BlitzReader reader) {
        int triggerBoxCount = reader.ReadInt();
        RMesh.TriggerBox[] triggerBoxes = new RMesh.TriggerBox[triggerBoxCount];
        foreach (int triggerBoxIndex in Enumerable.Range(0, triggerBoxCount)) {
            ImmutableArray<RMesh.InvisibleCollisionMesh> submeshes = ReadInvisibleCollisionMeshes(reader);
            string name = reader.ReadString();
            triggerBoxes[triggerBoxIndex] = new RMesh.TriggerBox(name, submeshes);
        }
        return triggerBoxes.ToImmutableArray();
    }
}

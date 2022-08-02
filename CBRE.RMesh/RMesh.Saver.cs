using System.Collections.Immutable;

namespace CBRE.RMesh;

public partial record RMesh {
    public static class Saver {
        public static void ToFile(RMesh rmesh, string filePath) {
            using BlitzWriter writer = new BlitzWriter(filePath);
            
            writer.WriteString(rmesh.HeaderString);

            WriteVisibleMeshes(writer, rmesh.VisibleMeshes);
            WriteInvisibleCollisionMeshes(writer, rmesh.InvisibleCollisionMeshes);

            if (rmesh.VisibleNoCollisionMeshes.HasValue) {
                WriteVisibleMeshes(writer, rmesh.VisibleNoCollisionMeshes.Value);
            }

            if (rmesh.TriggerBoxes.HasValue) {
                WriteTriggerBoxes(writer, rmesh.TriggerBoxes.Value);
            }
            
            WriteEntities(writer);
        }

        private static void WriteVisibleMeshes(BlitzWriter writer, in ImmutableArray<VisibleMesh> visibleMeshes) {
            writer.WriteInt(visibleMeshes.Length);

            foreach (var mesh in visibleMeshes) {
                void writeTextureInfo(byte textureType, string texturePath) {
                    writer.WriteByte(textureType);
                    if (textureType != 0) { writer.WriteString(texturePath); }
                }
                
                if (mesh.TextureBlendMode == VisibleMesh.BlendMode.Lightmapped) {
                    writeTextureInfo(2, mesh.LightmapTexture ?? throw new Exception("Blend mode is lightmapped but lightmap texture is null"));
                    writeTextureInfo(1, mesh.DiffuseTexture);
                } else {
                    writeTextureInfo((byte)(mesh.TextureBlendMode == VisibleMesh.BlendMode.Opaque ? 2 : 3), mesh.DiffuseTexture);
                    writeTextureInfo(0, "");
                }
                
                writer.WriteInt(mesh.Vertices.Length);
                if (mesh.Vertices.Length > UInt16.MaxValue) throw new Exception("Vertex overflow: " + mesh.DiffuseTexture);
                foreach (var vertex in mesh.Vertices) {
                    writer.WriteFloat(vertex.Position.X);
                    writer.WriteFloat(vertex.Position.Y);
                    writer.WriteFloat(vertex.Position.Z);
                    
                    writer.WriteFloat(vertex.DiffuseUv.X);
                    writer.WriteFloat(vertex.DiffuseUv.Y);
                    writer.WriteFloat(vertex.LightmapUv.X);
                    writer.WriteFloat(vertex.LightmapUv.Y);
                    
                    writer.WriteByte(vertex.Color.R);
                    writer.WriteByte(vertex.Color.G);
                    writer.WriteByte(vertex.Color.B);
                }
                
                writer.WriteInt(mesh.Triangles.Length);
                if (mesh.Triangles.Length > UInt16.MaxValue) throw new Exception("Vertex overflow: " + mesh.DiffuseTexture);
                foreach (var triangle in mesh.Triangles) {
                    writer.WriteInt(triangle.Index0);
                    writer.WriteInt(triangle.Index1);
                    writer.WriteInt(triangle.Index2);
                }
            }
        }

        private static void WriteInvisibleCollisionMeshes(BlitzWriter writer, in ImmutableArray<InvisibleCollisionMesh> invisibleCollisionMeshes) {
            writer.WriteInt(invisibleCollisionMeshes.Length);

            foreach (var mesh in invisibleCollisionMeshes) {
                writer.WriteInt(mesh.Vertices.Length);
                foreach (var vertex in mesh.Vertices) {
                    writer.WriteFloat(vertex.Position.X);
                    writer.WriteFloat(vertex.Position.Y);
                    writer.WriteFloat(vertex.Position.Z);
                }
                
                writer.WriteInt(mesh.Triangles.Length);
                foreach (var triangle in mesh.Triangles) {
                    writer.WriteInt(triangle.Index0);
                    writer.WriteInt(triangle.Index1);
                    writer.WriteInt(triangle.Index2);
                }
            }
        }

        private static void WriteTriggerBoxes(BlitzWriter writer, in ImmutableArray<TriggerBox> triggerBoxes) {
            writer.WriteInt(triggerBoxes.Length);

            foreach (var triggerBox in triggerBoxes) {
                WriteInvisibleCollisionMeshes(writer, triggerBox.SubMeshes);
                writer.WriteString(triggerBox.Name);
            }
        }

        private static void WriteEntities(BlitzWriter writer) {
            writer.WriteInt(0);
            //TODO: implement
        }
    }
}

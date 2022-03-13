using System;
using System.Collections.Immutable;
using System.Drawing;
using CBRE.DataStructures.Geometric;

namespace CBRE.RMesh;

public record RMesh(
    string Header,
    ImmutableArray<RMesh.VisibleMesh> VisibleMeshes,
    ImmutableArray<RMesh.InvisibleCollisionMesh> InvisibleCollisionMeshes,
    ImmutableArray<RMesh.VisibleMesh>? VisibleNoCollisionMeshes,
    ImmutableArray<RMesh.TriggerBox>? TriggerBoxes) { //TODO: move point entity handling code here
    public readonly record struct Triangle(
        UInt16 Index0,
        UInt16 Index1,
        UInt16 Index2);

    public record VisibleMesh(
        ImmutableArray<VisibleMesh.Vertex> Vertices,
        ImmutableArray<Triangle> Triangles,
        string DiffuseTexture,
        string? LightmapTexture,
        VisibleMesh.BlendMode TextureBlendMode) {
        public enum BlendMode {
            Opaque,
            Lightmapped,
            Translucent
        }
        
        public readonly record struct Vertex(
            Vector3F Position,
            Vector2F DiffuseUv,
            Vector2F LightmapUv,
            Color Color);
    }

    public record InvisibleCollisionMesh(
        ImmutableArray<InvisibleCollisionMesh.Vertex> Vertices,
        ImmutableArray<Triangle> Triangles) {
        public readonly record struct Vertex(
            Vector3F Position);
    }

    public record TriggerBox(
        string Name,
        ImmutableArray<InvisibleCollisionMesh> SubMeshes);
}

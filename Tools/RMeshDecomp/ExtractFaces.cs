using System.Diagnostics;
using System.Drawing;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.RMesh;

namespace RMeshDecomp;

public static class ExtractFaces {
    private const decimal precision = 1m;
    
    readonly struct Triangle {
        public readonly int[] Indices;
        public readonly RMesh.VisibleMesh.Vertex[] Vertices;
        public readonly PlaneF PlaneF;
        public readonly Plane Plane;
        
        public Triangle(RMesh.VisibleMesh mesh, RMesh.Triangle rmeshTriangle) {
            Indices = new[] {
                (int)rmeshTriangle.Index0,
                (int)rmeshTriangle.Index1,
                (int)rmeshTriangle.Index2
            };
            Vertices = Indices.Select(i => mesh.Vertices[i]).ToArray();
            PlaneF = new PlaneF(Vertices[0].Position, Vertices[1].Position, Vertices[2].Position);
            Plane = new Plane(new Vector3(PlaneF.Normal), (decimal)PlaneF.DistanceFromOrigin);
        }
    }

    readonly struct PlaneKey : IEquatable<PlaneKey> {
        public readonly Plane Plane;
        public PlaneKey(Plane plane) => Plane = plane;
        
        public static implicit operator Plane(PlaneKey key) => key.Plane;
        public static implicit operator PlaneKey(Plane plane) => new PlaneKey(plane);
        public static implicit operator PlaneKey(PlaneF plane) => new PlaneKey(
            new Plane(new Vector3(plane.Normal), (decimal)plane.DistanceFromOrigin));

        private const decimal normalFuzz = (precision * 0.001m);
        
        public override int GetHashCode() {
            return HashCode.Combine(
                (int)Math.Round(Plane.Normal.X / normalFuzz),
                (int)Math.Round(Plane.Normal.Y / normalFuzz),
                (int)Math.Round(Plane.Normal.Z / normalFuzz));
        }

        public IEnumerable<PlaneKey> Fuzz() {
            IEnumerable<(int X, int Y, int Z)> fuzzAmount()
                => Enumerable.Range(-1, 3)
                    .Zip(Enumerable.Range(-1, 3))
                    .Zip(Enumerable.Range(-1, 3))
                    .Select(tp => (tp.First.First, tp.First.Second, tp.Second));

            var plane = Plane;
            return fuzzAmount().Select(tp => (PlaneKey)new Plane(
                new Vector3(plane.Normal.X + (tp.X * normalFuzz),
                    plane.Normal.Y + (tp.Y * normalFuzz),
                    plane.Normal.Z + (tp.Z * normalFuzz)).Normalise(),
                plane.DistanceFromOrigin));
        }

        public override bool Equals(object? o) {
            return o is PlaneKey other && Equals(other);
        }

        public bool Equals(PlaneKey other)
            => Plane.Normal.EquivalentTo(other.Plane.Normal, normalFuzz)
                && Math.Abs(Plane.DistanceFromOrigin - other.Plane.DistanceFromOrigin) < precision;
    }
    
    public static void Invoke(RMesh.VisibleMesh mesh, HashSet<Face> faces) {
        
        int faceId = 600;
        Face createNewFace() {
            faceId++;
            return new Face(faceId);
        }
        
        RMesh.VisibleMesh.Vertex[] triangleVertices(RMesh.Triangle triangle)
            => new[] {
                mesh.Vertices[triangle.Index0],
                mesh.Vertices[triangle.Index1],
                mesh.Vertices[triangle.Index2]
            };

        bool tvEquivalentFvLoc(RMesh.VisibleMesh.Vertex tv, Vector3 fv)
            => tv.Position.EquivalentTo(new Vector3F(fv), (float)precision);

        bool tvEquivalentFv(RMesh.VisibleMesh.Vertex tv, Vertex fv)
            => tvEquivalentFvLoc(tv, fv.Location);

        bool triangleNeighborsFace(Triangle triangle, Face face) {
            if (triangle.Vertices.Any(v => face.Plane.OnPlane(new Vector3(v.Position), precision) != 0)) { return false; }

            int adjacentEdges = face.GetEdges().Count(edge
                => triangle.Vertices.Any(tv => tvEquivalentFvLoc(tv, edge.Start))
                   && triangle.Vertices.Any(tv => tvEquivalentFvLoc(tv, edge.End)));

            return adjacentEdges is > 0 and < 3;
        }

        bool isTriangleValid(RMesh.Triangle triangle) {
            var verts = triangleVertices(triangle);

            bool checkEdge(int i)
                => Math.Abs((verts[i].Position - verts[(i + 1) % 3].Position).Normalise()
                    .Dot((verts[(i + 1) % 3].Position - verts[(i + 2) % 3].Position).Normalise())) < 0.9999f;
            
            return !verts[0].Position.EquivalentTo(verts[1].Position, (float)precision)
                   && !verts[0].Position.EquivalentTo(verts[2].Position, (float)precision)
                   && !verts[1].Position.EquivalentTo(verts[2].Position, (float)precision)
                   && (checkEdge(0) || checkEdge(1) || checkEdge(2));
        }

        List<Triangle> pendingTriangles = mesh.Triangles
            .Where(isTriangleValid)
            .Where(t => triangleVertices(t).All(v => v.Color.ToArgb() == Color.White.ToArgb()))
            .Select(t => new Triangle(mesh, t))
            .ToList();
        
        var triangleGrouping = new List<(List<Triangle> Triangles, HashSet<int> Indices)>();

        for (int i = 0; i < pendingTriangles.Count; i++) {
            List<Triangle> trianglesInGroup = new List<Triangle>();
            trianglesInGroup.Add(pendingTriangles[i]);
            HashSet<int> indices = pendingTriangles[i].Indices.ToHashSet();
            for (int j = (i+1); j < pendingTriangles.Count; j++) {
                var intersection = indices.Intersect(pendingTriangles[j].Indices).ToArray();
                
                if (!intersection.Any()) { continue; }

                indices.UnionWith(pendingTriangles[j].Indices);
                trianglesInGroup.Add(pendingTriangles[j]);
                pendingTriangles.RemoveAt(j);
                j--;
            }
            triangleGrouping.Add((trianglesInGroup, indices));
        }
        pendingTriangles.Clear();

        bool addTriangleToFace(Triangle triangle, Face face) {
            var edges = face.GetEdges().ToArray();

            bool vertexEquals(RMesh.VisibleMesh.Vertex vertex, Vector3 point)
                => point.EquivalentTo(new Vector3(vertex.Position), precision);

            var matchingEdge = edges.FirstOrDefault(e =>
                triangle.Vertices.Count(v => vertexEquals(v, e.Start) || vertexEquals(v, e.End)) == 2);

            if (matchingEdge == null) { return false; }

            var extraVertex = triangle.Vertices.First(v
                => !vertexEquals(v, matchingEdge.Start) && !vertexEquals(v, matchingEdge.End));
            
            face.Vertices.Insert(Array.IndexOf(edges, matchingEdge)+1, new Vertex(new Vector3(extraVertex.Position), face) {
                TextureU = (decimal)extraVertex.DiffuseUv.X,
                TextureV = (decimal)extraVertex.DiffuseUv.Y,
            });

            return true;
        }

        var newFaceList = new List<Face>();
        
        foreach (var (trianglesPrime, _) in triangleGrouping) {
            IEnumerable<List<Triangle>> subgroupings = new [] { trianglesPrime };
            if (trianglesPrime.Any(t1
                => trianglesPrime.Any(t2
                    => !t2.Plane.Normal.EquivalentTo(t1.Plane.Normal, precision * 0.01m)))) {
                subgroupings = trianglesPrime.GroupBy(t => new PlaneKey(t.Plane)).Select(e => e.ToList());
            }

            foreach (var triangles in subgroupings) {
                var newFace = createNewFace();
                newFace.Vertices = triangles[0].Vertices.Select(v => new Vertex(new Vector3(v.Position), newFace) {
                    TextureU = (decimal)v.DiffuseUv.X,
                    TextureV = (decimal)v.DiffuseUv.Y,
                })
                    .ToList();
                triangles.RemoveAt(0);
                while (triangles.Count > 0) {
                    var toRemove = triangles.FindIndex(t => addTriangleToFace(t, newFace));
                    triangles.RemoveAt(toRemove);
                }
                newFaceList.Add(newFace);
            }
        }
        
        var newFaces = new Dictionary<PlaneKey, List<Face>>();

        bool tryExtendFace() {
            if (!newFaces.Any()) { return false; }
            
            foreach (var triIndex in Enumerable.Range(0, pendingTriangles.Count)) {
                var triangle = pendingTriangles[triIndex];
                var planeKey = (PlaneKey)triangle.PlaneF;
                var fuzzKeys = planeKey.Fuzz().ToArray();
                if (!fuzzKeys.Any(newFaces.ContainsKey)) { continue; }
                var facesOnPlane =
                    fuzzKeys.SelectMany(k => newFaces.TryGetValue(k, out var fs) ? fs : Enumerable.Empty<Face>());
                var face = facesOnPlane.FirstOrDefault(f => triangleNeighborsFace(triangle, f));
                if (face == null) { continue; }

                pendingTriangles.RemoveAt(triIndex);

                var finalVertices = face.Vertices.ToList();

                IEnumerable<Line> getEdges()
                    => Enumerable.Range(0, finalVertices.Count)
                        .Select(i => new Line(finalVertices[i].Location,
                            finalVertices[(i + 1) % finalVertices.Count].Location));

                var triVerts = triangle.Vertices;

                bool strayVertFound = false;
                RMesh.VisibleMesh.Vertex vertexToAdd = default;
                foreach (var vert in triVerts) {
                    if (face.Vertices.Any(fv => tvEquivalentFv(vert, fv))) {
                        continue;
                    }

                    vertexToAdd = vert;
                    strayVertFound = true;
                    break;
                }

                if (!strayVertFound) { return true; }

                (int Index, Line Edge)[] edges = Enumerable.Range(0, finalVertices.Count).Zip(getEdges()).ToArray();
                var vPosDecimal = new Vector3(vertexToAdd.Position);
                var edgeToSplit = edges
                    .First(e
                        => triVerts.Any(v => v.Position.EquivalentTo(new Vector3F(e.Edge.Start), (float)precision))
                            && triVerts.Any(v => v.Position.EquivalentTo(new Vector3F(e.Edge.End), (float)precision)));
                var newFaceVertex = new Vertex(vPosDecimal, face) {
                    TextureU = (decimal)vertexToAdd.DiffuseUv.X,
                    TextureV = (decimal)vertexToAdd.DiffuseUv.Y,
                };
                finalVertices.Insert(edgeToSplit.Index + 1, newFaceVertex);

                face.Vertices = finalVertices;
                face.AlignTextureToFace();

                return true;
            }
            return false;
        }

        while (false && pendingTriangles.Count > 0) {
            if (!tryExtendFace()) {
                var newFace = createNewFace();
                newFace.Vertices = new List<Vertex>();
                var triangle = pendingTriangles[0]; pendingTriangles.RemoveAt(0);
                newFace.Vertices.AddRange(triangle.Vertices.Select(v => new Vertex(new Vector3(v.Position), newFace) {
                    TextureU = (decimal)v.DiffuseUv.X,
                    TextureV = (decimal)v.DiffuseUv.Y,
                }));
                newFace.Plane = new Plane(newFace.Vertices[0].Location, newFace.Vertices[1].Location, newFace.Vertices[2].Location);

                var planeKey = new PlaneKey(newFace.Plane);
                if (!newFaces.ContainsKey(planeKey)) { newFaces[planeKey] = new List<Face>(); }
                
                newFaces[planeKey].Add(newFace);
            }
        }

        IEnumerable<Face> splitNonConvex(Face face) {
            for (int i = 0; i < face.Vertices.Count; i++) {
                var toRemove = face.Vertices.Skip(i+1)
                    .Where(v => v.Location.EquivalentTo(face.Vertices[i].Location, precision))
                    .ToArray();
                /*if (toRemove.Any()) {
                    Debugger.Break();
                }*/
                face.Vertices.RemoveAll(v => toRemove.Contains(v));
            }

            if (face.Vertices.Count < 3) {
                return Enumerable.Empty<Face>();
            }
            
            if (face.IsConvex(precision * 0.01m) && !face.HasColinearEdges(precision * 0.01m)) {
                return new [] { face };
            }
            
            Polygon currPolygon;
            
            bool polygonIsValid()
                => currPolygon.IsConvex(precision * 0.01m) && !currPolygon.HasColinearEdges(precision * 0.01m);
            
            int initialShift = 0;
            do {
                if (initialShift >= face.Vertices.Count) { return Enumerable.Empty<Face>(); }
                if (initialShift > 0) {
                    var v = face.Vertices[0];
                    face.Vertices.RemoveAt(0);
                    face.Vertices.Add(v);
                }
                currPolygon = new Polygon(
                    new[] {
                        face.Vertices[0].Location,
                        face.Vertices[1].Location,
                        face.Vertices[2].Location
                    });
                initialShift++;
            } while (!polygonIsValid());
            
            int endIndex = 2;
            for (int i = 3; i < face.Vertices.Count; i++) {
                currPolygon.Vertices.Add(face.Vertices[i].Location);
                
                if (polygonIsValid()) {
                    endIndex = i;
                    continue;
                }

                currPolygon.Vertices.RemoveAt(currPolygon.Vertices.Count-1);
                if (!polygonIsValid()) { throw new Exception("fuck"); }
                break;
            }

            int startIndex = 0;
            for (int i = face.Vertices.Count-1; i > endIndex; i--) {
                currPolygon.Vertices.Insert(0, face.Vertices[i].Location);

                if (polygonIsValid()) {
                    startIndex = i;
                    continue;
                }

                currPolygon.Vertices.RemoveAt(0);
                if (!polygonIsValid()) { throw new Exception("fuck"); }
                break;
            }

            List<Face> splitFaces = new List<Face>();
            var knownConvexFace = createNewFace();
            var otherFace = createNewFace();
            var vertArray = face.Vertices.ToArray();

            for (int i = 0; i < vertArray.Length; i++) {
                Vertex cloneForFace(Vertex v, Face f) {
                    var clone = v.Clone();
                    clone.Parent = f;
                    return clone;
                }

                bool partOfConvexFace = startIndex < endIndex
                    ? startIndex <= i && i <= endIndex
                    : startIndex <= i || i <= endIndex;
                bool partOfOtherFace = !partOfConvexFace || i == startIndex || i == endIndex;
                
                if (partOfConvexFace) { knownConvexFace.Vertices.Add(cloneForFace(vertArray[i], knownConvexFace)); }
                if (partOfOtherFace) { otherFace.Vertices.Add(cloneForFace(vertArray[i], otherFace)); }
            }
            
            knownConvexFace.Plane = new Plane(
                knownConvexFace.Vertices[0].Location,
                knownConvexFace.Vertices[1].Location,
                knownConvexFace.Vertices[2].Location);
            otherFace.Plane = knownConvexFace.Plane.Clone();

            if (knownConvexFace.HasColinearEdges(precision * 0.01m)) {
                Debugger.Break();
            }

            var kcfTris = knownConvexFace.GetTriangles().Select(t => new Plane(t[1].Location, t[0].Location, t[2].Location)).ToArray();
            if (!kcfTris.All(t => t
                    .EquivalentTo(knownConvexFace.Plane,
                        precision * 0.1m))) {
                //Debugger.Break();
            }
            
            knownConvexFace.UpdateBoundingBox();
            otherFace.UpdateBoundingBox();
            
            splitFaces.Add(knownConvexFace);
            if (otherFace.Vertices.Count >= 3) {
                splitFaces.AddRange(splitNonConvex(otherFace));
            }
            return splitFaces;
        }
        
        newFaces.Add(new PlaneKey(new Plane(Vector3.UnitX, 0m)), newFaceList);
        
        foreach (var kvp in newFaces) {
            foreach (var face in kvp.Value.Where(f => !f.IsConvex(precision * 0.1m) || f.HasColinearEdges(precision * 0.1m)).ToList()) {
                kvp.Value.Remove(face); kvp.Value.AddRange(splitNonConvex(face));
            }
        }

        foreach (var newFace in newFaces.SelectMany(kvp => kvp.Value))
        {
            foreach (var vert in newFace.Vertices) {
                vert.Location = vert.Location.XZY();
            }
            newFace.UpdateBoundingBox();
            newFace.Plane = new Plane(
                newFace.Vertices[0].Location,
                newFace.Vertices[1].Location,
                newFace.Vertices[2].Location);
        }
        faces.UnionWith(newFaces.SelectMany(kvp => kvp.Value));
    }
}

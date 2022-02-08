using System;
using System.Collections.Generic;
using System.Linq;
using CBRE.DataStructures.Geometric;
using CBRE.Extensions;

namespace CBRE.Graphics {
    public static class ShapeGenerator {
        public static List<Vector3[]> Cylinder(Box box, int numSides, int roundDecimals) {
            // Cylinders can be elliptical so use both major and minor rather than just the radius
            // NOTE: when a low number (< 10ish) of faces are selected this will cause the cylinder to not touch all the edges of the box.
            var width = box.Width;
            var length = box.Length;
            var height = box.Height;
            var major = width / 2;
            var minor = length / 2;
            var angle = 2 * DMath.PI / numSides;

            // Calculate the X and Y points for the ellipse
            var points = new Vector3[numSides];
            for (var i = 0; i < numSides; i++) {
                var a = i * angle;
                var xval = box.Center.X + major * DMath.Cos(a);
                var yval = box.Center.Y + minor * DMath.Sin(a);
                var zval = box.Start.Z;
                points[i] = new Vector3(xval, yval, zval).Round(roundDecimals);
            }

            var faces = new List<Vector3[]>();

            // Add the vertical faces
            var z = new Vector3(0, 0, height).Round(roundDecimals);
            for (var i = 0; i < numSides; i++) {
                var next = (i + 1) % numSides;
                faces.Add(new[] { points[i], points[i] + z, points[next] + z, points[next] });
            }
            // Add the elliptical top and bottom faces
            faces.Add(points.ToArray());
            faces.Add(points.Select(x => x + z).Reverse().ToArray());

            return faces;
        }

        public static List<Vector3[]> Cylinder(Line centerLine, decimal radius, int numSides, int roundDecimals) {
            Box box = new Box(
                new Vector3(-radius, -radius, 0m),
                new Vector3(radius, radius, centerLine.Length()));

            Vector3 lineDir = centerLine.Direction();
            Plane transformationPlane = new Plane(lineDir, Vector3.Zero);
            Vector3 transformedX = transformationPlane.Project(Math.Abs(lineDir.X)<0.99m ? Vector3.UnitX : -Vector3.UnitZ).Normalise();
            Vector3 transformedY = transformationPlane.Project(Math.Abs(lineDir.Y)<0.99m ? Vector3.UnitY : -Vector3.UnitZ).Normalise();
            
            var faces = Cylinder(box, numSides, roundDecimals);
            foreach (var face in faces) {
                for (int i = 0; i < face.Length; i++) {
                    face[i] =
                        centerLine.Start
                        + transformedX * face[i].X
                        + transformedY * face[i].Y
                        + lineDir * face[i].Z;
                }
            }
            return faces;
        }
    }
}

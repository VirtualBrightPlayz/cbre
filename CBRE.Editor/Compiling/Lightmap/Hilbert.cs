using System;

namespace CBRE.Editor.Compiling.Lightmap;

static class Hilbert {
    private static int Wrap(int i) => (4 + (i % 4)) % 4;

    private static void CalculateNextOrientationAndReverse(
        in int quadrant, in int orientation, in bool reverse, out int nextOrientation, out bool nextReverse
    ) {
        nextOrientation = Wrap(orientation + (quadrant switch {
            0 => 1,
            1 => 0,
            2 => 0,
            3 => 3,
            _ => throw new InvalidOperationException()
        }) * (reverse ? -1 : 1));
        nextReverse = reverse ^ (quadrant switch {
            0 => true,
            1 => false,
            2 => false,
            3 => true,
            _ => throw new InvalidOperationException()
        });
    }

    public static LightmapGroup.UvPairInt IndexToPoint(in int index, in int sideLength, in int orientation, in bool reverse) {
        if (sideLength < 2) { return (0, 0); }

        int quadrant = index / (sideLength * sideLength / 4);
        LightmapGroup.UvPairInt quadrantTopLeftCorner = Wrap((reverse ? 3 - quadrant : quadrant) + orientation) switch {
            0 => (0, 0),
            1 => (sideLength / 2, 0),
            2 => (sideLength / 2, sideLength / 2),
            3 => (0, sideLength / 2),
            _ => throw new InvalidOperationException()
        };

        if (sideLength == 2) { return quadrantTopLeftCorner; }

        CalculateNextOrientationAndReverse(
            quadrant,
            orientation,
            reverse,
            out int nextOrientation,
            out bool nextReverse);

        LightmapGroup.UvPairInt subPoint = IndexToPoint(index - quadrant * (sideLength * sideLength / 4), sideLength / 2,
            nextOrientation, nextReverse);

        return (quadrantTopLeftCorner.U + subPoint.U, quadrantTopLeftCorner.V + subPoint.V);
    }

    public static int PointToIndex(in LightmapGroup.UvPairInt point, in int sideLength, in int orientation, in bool reverse) {
        if (sideLength < 2) { return 0; }

        LightmapGroup.UvPairInt quadrantPoint = (point.U / (sideLength / 2), point.V / (sideLength / 2));
        int quadrant = (quadrantPoint switch {
            (0, 0) => 0,
            (1, 0) => 1,
            (1, 1) => 2,
            (0, 1) => 3,
            _ => throw new InvalidOperationException()
        });
        quadrant = Wrap((reverse ? 3 - quadrant : quadrant) + orientation);

        if (sideLength == 2) { return quadrant; }

        CalculateNextOrientationAndReverse(
            quadrant,
            orientation,
            reverse,
            out int nextOrientation,
            out bool nextReverse);

        int subIndex
            = PointToIndex(
                (point.U - quadrantPoint.U * (sideLength / 2), point.V - quadrantPoint.V * (sideLength / 2)),
                sideLength / 2, nextOrientation, nextReverse);

        return quadrant * sideLength * sideLength / 4 + subIndex;
    }
}

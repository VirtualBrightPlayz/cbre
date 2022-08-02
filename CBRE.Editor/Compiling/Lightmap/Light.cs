﻿using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.DataStructures.Transformations;
using CBRE.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CBRE.Editor.Compiling.Lightmap {
    struct Light {
        public Vector3F Color;
        public float Intensity;
        public bool HasSprite;
        public Vector3F Origin;
        public float Range;

        public Vector3F? Direction;
        public float? innerCos;
        public float? outerCos;

        public static void FindLights(Map map, out List<Light> lightEntities) {
            Predicate<string> parseBooleanProperty = (prop) => {
                return prop.Equals("yes", StringComparison.OrdinalIgnoreCase) || prop.Equals("true", StringComparison.OrdinalIgnoreCase);
            };

            lightEntities = new List<Light>();
            lightEntities.AddRange(map.WorldSpawn.Find(q => q.ClassName == "light").OfType<Entity>()
                .Select(x => {
                    float range;
                    if (!float.TryParse(x.EntityData.GetPropertyValue("range"), out range)) {
                        range = 100.0f;
                    }
                    float intensity;
                    if (!float.TryParse(x.EntityData.GetPropertyValue("intensity"), out intensity)) {
                        intensity = 1.0f;
                    }
                    bool hasSprite = parseBooleanProperty(x.EntityData.GetPropertyValue("hassprite") ?? "true");

                    return new Light() {
                        Origin = new Vector3F(x.Origin),
                        Range = range,
                        Color = new Vector3F(x.EntityData.GetPropertyVector3("color")),
                        Intensity = intensity,
                        HasSprite = hasSprite,
                        Direction = null,
                        innerCos = null,
                        outerCos = null
                    };
                }));
            lightEntities.AddRange(map.WorldSpawn.Find(q => q.ClassName == "spotlight").OfType<Entity>()
                .Select(x => {
                    float range;
                    if (!float.TryParse(x.EntityData.GetPropertyValue("range"), out range)) {
                        range = 100.0f;
                    }
                    float intensity;
                    if (!float.TryParse(x.EntityData.GetPropertyValue("intensity"), out intensity)) {
                        intensity = 1.0f;
                    }
                    bool hasSprite = parseBooleanProperty(x.EntityData.GetPropertyValue("hassprite") ?? "true");
                    float innerCos = 0.5f;
                    if (float.TryParse(x.EntityData.GetPropertyValue("innerconeangle"), out innerCos)) {
                        innerCos = (float)Math.Cos(innerCos * (float)Math.PI / 180.0f);
                    }
                    float outerCos = 0.75f;
                    if (float.TryParse(x.EntityData.GetPropertyValue("outerconeangle"), out outerCos)) {
                        outerCos = (float)Math.Cos(outerCos * (float)Math.PI / 180.0f);
                    }

                    Light light = new Light() {
                        Origin = new Vector3F(x.Origin),
                        Range = range,
                        Color = new Vector3F(x.EntityData.GetPropertyVector3("color")),
                        Intensity = intensity,
                        HasSprite = hasSprite,
                        Direction = null,
                        innerCos = innerCos,
                        outerCos = outerCos
                    };

                    Vector3 angles = x.EntityData.GetPropertyVector3("angles");

                    Matrix pitch = Matrix.Rotation(Quaternion.EulerAngles(DMath.DegreesToRadians(angles.X), 0, 0));
                    Matrix yaw = Matrix.Rotation(Quaternion.EulerAngles(0, 0, -DMath.DegreesToRadians(angles.Y)));
                    Matrix roll = Matrix.Rotation(Quaternion.EulerAngles(0, DMath.DegreesToRadians(angles.Z), 0));

                    var m = new UnitMatrixMult(yaw * roll * pitch);

                    light.Direction = new Vector3F(m.Transform(Vector3.UnitY)).Normalise();
                    //TODO: make sure this matches 3dws

                    return light;
                }));
        }
    }
}

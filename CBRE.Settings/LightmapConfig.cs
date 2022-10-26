namespace CBRE.Settings {
    public class LightmapConfig {
        public static float DownscaleFactor { get; set; }
        public static int PlaneMargin { get; set; }
        public static int ShadowTextureDims { get; set; }
        public static int TextureDims { get; set; }
        public static int BlurRadius { get; set; }

        public static int AmbientColorR { get; set; }
        public static int AmbientColorG { get; set; }
        public static int AmbientColorB { get; set; }

        public static float AmbientNormalX { get; set; }
        public static float AmbientNormalY { get; set; }
        public static float AmbientNormalZ { get; set; }

        public static bool BakeModels { get; set; }
        public static bool BakeModelLightmaps { get; set; }
        public static bool ComputeShadows { get; set; }
        public static float BakeGamma { get; set; }
        public static float HitDistanceSquared { get; set; }

        static LightmapConfig() {
            DownscaleFactor = 15;
            PlaneMargin = 1;
            ShadowTextureDims = 1024;
            TextureDims = 512;
            BlurRadius = 2;

            AmbientColorR = 30;
            AmbientColorG = 30;
            AmbientColorB = 30;

            AmbientNormalX = 1.0f;
            AmbientNormalY = 2.0f;
            AmbientNormalZ = 3.0f;

            BakeModels = false;
            BakeModelLightmaps = false;
            ComputeShadows = true;
            BakeGamma = 1.0f;

            HitDistanceSquared = 100f;
        }
    }
}

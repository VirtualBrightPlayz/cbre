using System.Globalization;

namespace CBRE.Common {
    public static class PrimitiveConversion {
        
        public static int ParseInt(string str)
            => int.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);

        public static float ParseFloat(string str)
            => float.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);

        public static double ParseDouble(string str)
            => double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
        
        public static decimal ParseDecimal(string str)
            => decimal.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);

    }
}

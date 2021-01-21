using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace CBRE.Common {
    public static class XMLExtensions {
        public static object GetAttributeObject(XAttribute attribute) {
            if (attribute == null) return null;

            return ParseToObject(attribute.Value.ToString());
        }

        public static object ParseToObject(string value) {
            float floatVal;
            int intVal;
            if (value.Contains(".") && Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatVal)) {
                return floatVal;
            }
            if (Int32.TryParse(value, out intVal)) {
                return intVal;
            }

            string lowerTrimmedVal = value.ToLowerInvariant().Trim();
            if (lowerTrimmedVal == "true") {
                return true;
            }
            if (lowerTrimmedVal == "false") {
                return false;
            }

            return value;
        }


        public static string GetAttributeString(this XElement element, string name, string defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;
            return GetAttributeString(element.Attribute(name), defaultValue);
        }

        private static string GetAttributeString(XAttribute attribute, string defaultValue) {
            string value = attribute.Value;
            return String.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static string[] GetAttributeStringArray(this XElement element, string name, string[] defaultValue, bool trim = true, bool convertToLowerInvariant = false) {
            if (element?.Attribute(name) == null) return defaultValue;

            string stringValue = element.Attribute(name).Value;
            if (string.IsNullOrEmpty(stringValue)) return defaultValue;

            string[] splitValue = stringValue.Split(',', '，');

            if (convertToLowerInvariant) {
                for (int i = 0; i < splitValue.Length; i++) {
                    splitValue[i] = splitValue[i].ToLowerInvariant();
                }
            }
            if (trim) {
                for (int i = 0; i < splitValue.Length; i++) {
                    splitValue[i] = splitValue[i].Trim();
                }
            }

            return splitValue;
        }

        public static float GetAttributeFloat(this XElement element, float defaultValue, params string[] matchingAttributeName) {
            if (element == null) return defaultValue;

            foreach (string name in matchingAttributeName) {
                if (element.Attribute(name) == null) continue;

                float val;
                try {
                    string strVal = element.Attribute(name).Value;
                    if (strVal.LastOrDefault() == 'f') {
                        strVal = strVal.Substring(0, strVal.Length - 1);
                    }
                    val = float.Parse(strVal, CultureInfo.InvariantCulture);
                } catch (Exception e) {
                    Debug.WriteLine("Error in " + element + "!", e);
                    continue;
                }
                return val;
            }

            return defaultValue;
        }

        public static float GetAttributeFloat(this XElement element, string name, float defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            float val = defaultValue;
            try {
                string strVal = element.Attribute(name).Value;
                if (strVal.LastOrDefault() == 'f') {
                    strVal = strVal.Substring(0, strVal.Length - 1);
                }
                val = float.Parse(strVal, CultureInfo.InvariantCulture);
            } catch (Exception e) {
                Debug.WriteLine("Error in " + element + "!", e);
            }

            return val;
        }

        public static float GetAttributeFloat(this XAttribute attribute, float defaultValue) {
            if (attribute == null) return defaultValue;

            float val = defaultValue;

            try {
                string strVal = attribute.Value;
                if (strVal.LastOrDefault() == 'f') {
                    strVal = strVal.Substring(0, strVal.Length - 1);
                }
                val = float.Parse(strVal, CultureInfo.InvariantCulture);
            } catch (Exception e) {
                Debug.WriteLine("Error in " + attribute + "! ", e);
            }

            return val;
        }

        public static float[] GetAttributeFloatArray(this XElement element, string name, float[] defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            string stringValue = element.Attribute(name).Value;
            if (string.IsNullOrEmpty(stringValue)) return defaultValue;

            string[] splitValue = stringValue.Split(',');
            float[] floatValue = new float[splitValue.Length];
            for (int i = 0; i < splitValue.Length; i++) {
                try {
                    string strVal = splitValue[i];
                    if (strVal.LastOrDefault() == 'f') {
                        strVal = strVal.Substring(0, strVal.Length - 1);
                    }
                    floatValue[i] = float.Parse(strVal, CultureInfo.InvariantCulture);
                } catch (Exception e) {
                    Debug.WriteLine("Error in " + element + "! ", e);
                }
            }

            return floatValue;
        }

        public static int GetAttributeInt(this XElement element, string name, int defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            int val = defaultValue;

            try {
                val = Int32.Parse(element.Attribute(name).Value, CultureInfo.InvariantCulture);
            } catch (Exception e) {
                Debug.WriteLine("Error in " + element + "! ", e);
            }

            return val;
        }

        public static uint GetAttributeUInt(this XElement element, string name, uint defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            uint val = defaultValue;

            try {
                val = UInt32.Parse(element.Attribute(name).Value);
            } catch (Exception e) {
                Debug.WriteLine("Error in " + element + "! ", e);
            }

            return val;
        }

        public static UInt64 GetAttributeUInt64(this XElement element, string name, UInt64 defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            UInt64 val = defaultValue;

            try {
                val = UInt64.Parse(element.Attribute(name).Value);
            } catch (Exception e) {
                Debug.WriteLine("Error in " + element + "! ", e);
            }

            return val;
        }

        public static int[] GetAttributeIntArray(this XElement element, string name, int[] defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            string stringValue = element.Attribute(name).Value;
            if (string.IsNullOrEmpty(stringValue)) return defaultValue;

            string[] splitValue = stringValue.Split(',');
            int[] intValue = new int[splitValue.Length];
            for (int i = 0; i < splitValue.Length; i++) {
                try {
                    int val = Int32.Parse(splitValue[i]);
                    intValue[i] = val;
                } catch (Exception e) {
                    Debug.WriteLine("Error in " + element + "! ", e);
                }
            }

            return intValue;
        }
        public static ushort[] GetAttributeUshortArray(this XElement element, string name, ushort[] defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;

            string stringValue = element.Attribute(name).Value;
            if (string.IsNullOrEmpty(stringValue)) return defaultValue;

            string[] splitValue = stringValue.Split(',');
            ushort[] ushortValue = new ushort[splitValue.Length];
            for (int i = 0; i < splitValue.Length; i++) {
                try {
                    ushort val = ushort.Parse(splitValue[i]);
                    ushortValue[i] = val;
                } catch (Exception e) {
                    Debug.WriteLine("Error in " + element + "! ", e);
                }
            }

            return ushortValue;
        }

        public static bool GetAttributeBool(this XElement element, string name, bool defaultValue) {
            if (element?.Attribute(name) == null) return defaultValue;
            return element.Attribute(name).GetAttributeBool(defaultValue);
        }

        public static bool GetAttributeBool(this XAttribute attribute, bool defaultValue) {
            if (attribute == null) return defaultValue;

            string val = attribute.Value.ToLowerInvariant().Trim();
            if (val == "true") {
                return true;
            }
            if (val == "false") {
                return false;
            }

            Debug.WriteLine("Error in " + attribute.Value.ToString() + "! \"" + val + "\" is not a valid boolean value");
            return false;
        }

        public static float[] ParseFloatArray(string[] stringArray) {
            if (stringArray == null || stringArray.Length == 0) return null;

            float[] floatArray = new float[stringArray.Length];
            for (int i = 0; i < floatArray.Length; i++) {
                floatArray[i] = 0.0f;
                Single.TryParse(stringArray[i], NumberStyles.Float, CultureInfo.InvariantCulture, out floatArray[i]);
            }

            return floatArray;
        }

        public static XElement FirstElement(this XElement element) => element.Elements().FirstOrDefault();

        public static XAttribute GetAttribute(this XElement element, string name, StringComparison comparisonMethod = StringComparison.OrdinalIgnoreCase) => element.GetAttribute(a => a.Name.ToString().Equals(name, comparisonMethod));

        public static XAttribute GetAttribute(this XElement element, Func<XAttribute, bool> predicate) => element.Attributes().FirstOrDefault(predicate);

        /// <summary>
        /// Returns the first child element that matches the name using the provided comparison method.
        /// </summary>
        public static XElement GetChildElement(this XContainer container, string name, StringComparison comparisonMethod = StringComparison.OrdinalIgnoreCase) => container.Elements().FirstOrDefault(e => e.Name.ToString().Equals(name, comparisonMethod));

        /// <summary>
        /// Returns all child elements that match the name using the provided comparison method.
        /// </summary>
        public static IEnumerable<XElement> GetChildElements(this XContainer container, string name, StringComparison comparisonMethod = StringComparison.OrdinalIgnoreCase) => container.Elements().Where(e => e.Name.ToString().Equals(name, comparisonMethod));
    }
}

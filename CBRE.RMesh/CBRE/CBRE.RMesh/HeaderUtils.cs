namespace CBRE.RMesh;

public partial record RMesh {
    public static class HeaderUtils {
        public const string HeaderBase = "RoomMesh";

        [Flags]
        public enum HeaderSuffix {
            None = 0x0,
            HasTriggerBox = 0x1,
            HasNoColl = 0x2
        }

        public static string EnumToString(HeaderSuffix suffixes) {
            string retVal = HeaderBase;
            
            foreach (var possibleSuffix in Enum.GetValues<HeaderSuffix>()) {
                if (possibleSuffix == HeaderSuffix.None) { continue; }
                if (suffixes.HasFlag(possibleSuffix)) { retVal += $".{possibleSuffix}"; }
            }
            
            return retVal;
        }

        public static bool IsHeaderValid(string header, out RMesh.HeaderUtils.HeaderSuffix headerSuffixes) {
            string[] split = header.Split('.');
            headerSuffixes = HeaderSuffix.None;

            if (split[0] != HeaderBase) { return false; }

            foreach (string part in split.Skip(1)) {
                if (!Enum.TryParse<HeaderSuffix>(part, out var val)) {
                    headerSuffixes = HeaderSuffix.None;
                    return false;
                }

                headerSuffixes |= val;
            }

            return true;
        }
    }
}

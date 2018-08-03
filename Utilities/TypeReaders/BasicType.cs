using System.ComponentModel;

namespace DiscordTestBot
{
    public enum BasicType {
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Bool,
        String
    }

    public static class BasicTypeHelper {
        private delegate bool Parser<T>(string input, out T value);
        private static bool TryParse<T>(string input, out object value, Parser<T> parser) {
            if (parser(input, out T v)) {
                value = v;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public static bool TryParseValue(this BasicType type, string input, out object value) {
            string orig = input;
            input = input.ToLowerInvariant();
            switch(type) {
                case BasicType.Byte:
                    return TryParse<byte>(input, out value, byte.TryParse);
                case BasicType.SByte:
                    return TryParse<sbyte>(input, out value, sbyte.TryParse);
                case BasicType.Short:
                    return TryParse<short>(input, out value, short.TryParse);
                case BasicType.UShort:
                    return TryParse<ushort>(input, out value, ushort.TryParse);
                case BasicType.Int:
                    return TryParse<int>(input, out value, int.TryParse);
                case BasicType.UInt:
                    return TryParse<uint>(input, out value, uint.TryParse);
                case BasicType.Long:
                    return TryParse<long>(input, out value, long.TryParse);
                case BasicType.ULong:
                    return TryParse<ulong>(input, out value, ulong.TryParse);
                case BasicType.Float:
                    return TryParse<float>(input, out value, float.TryParse);
                case BasicType.Double:
                    return TryParse<double>(input, out value, double.TryParse);
                case BasicType.Bool:
                    return TryParse<bool>(input, out value, bool.TryParse);
                case BasicType.String:
                    value = orig;
                    return true;
            }
            value = null;
            return false;
        }
    }
}
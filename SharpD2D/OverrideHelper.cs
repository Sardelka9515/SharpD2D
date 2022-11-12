using System;
using System.Text;

namespace SharpD2D
{
    internal static class OverrideHelper
    {
        public static int HashCodes(params int[] hashCodes)
        {
            if (hashCodes == null) throw new ArgumentNullException(nameof(hashCodes));
            if (hashCodes.Length == 0) throw new ArgumentOutOfRangeException(nameof(hashCodes));

            unchecked
            {
                var hash = 17;

                foreach (var code in hashCodes) hash = hash * 23 + code;

                return hash;
            }
        }

        public static string ToString(params string[] strings)
        {
            if (strings == null) throw new ArgumentNullException(nameof(strings));
            if (strings.Length == 0 || strings.Length % 2 != 0) throw new ArgumentOutOfRangeException(nameof(strings));

            var sb = new StringBuilder(16);

            sb.Append("{ ");

            for (var i = 0; i < strings.Length - 1; i += 2)
            {
                var name = strings[i];
                var value = strings[i + 1];

                if (name == null)
                {
                    if (value == null)
                        sb.Append("null");
                    else
                        sb.Append(value);
                }
                else if (value == null)
                {
                    sb.Append(name).Append(": null");
                }
                else
                {
                    sb.Append(name).Append(": ").Append(value);
                }

                sb.Append(", ");
            }

            sb.Length -= 2;

            sb.Append(" }");

            return sb.ToString();
        }
    }
}
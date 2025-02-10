using System;
using System.Text;
using System.Text.RegularExpressions;

namespace nspector.Common.CustomSettings
{
    [Serializable]
    public class CustomSettingValue
    {
        private static readonly Regex NonHexCharactersRegex = new Regex(@"[^0-9A-Fa-f]", RegexOptions.Compiled);
        private static readonly Regex HexStringRegex = new Regex(@"^[0-9A-Fa-f]+$", RegexOptions.Compiled);

        private ulong? _cachedSettingValue;

        public string UserfriendlyName { get; set; } = string.Empty;
        public string HexValue { get; set; } = string.Empty;

        internal ulong SettingValue
        {
            get
            {
                if (_cachedSettingValue == null && !string.IsNullOrEmpty(HexValue))
                {
                    string hexValue = HexValue.Trim();
                    hexValue = StripAndValidateHex(hexValue);
                    _cachedSettingValue = Convert.ToUInt64(hexValue, 16);
                }
                return _cachedSettingValue ?? throw new InvalidOperationException("HexValue cannot be null or empty.");
            }
        }

        private string StripAndValidateHex(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new FormatException("Hexadecimal value cannot be null or empty.");
            }

            var strippedValue = new StringBuilder();
            foreach (char c in value)
            {
                if (Uri.IsHexDigit(c))
                {
                    strippedValue.Append(c);
                }
            }

            if (strippedValue.Length == 0 || !HexStringRegex.IsMatch(strippedValue.ToString()))
            {
                throw new FormatException($"Invalid hexadecimal value: {value}");
            }

            return strippedValue.ToString();
        }
    }
}
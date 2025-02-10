using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace nspector.Common.CustomSettings
{
    [Serializable]
    public class CustomSetting
    {
        private static readonly Regex NonHexCharactersRegex = new Regex(@"[^0-9A-Fa-f]", RegexOptions.Compiled);
        private static readonly Regex HexStringRegex = new Regex(@"^[0-9A-Fa-f]+$", RegexOptions.Compiled);

        public string UserfriendlyName { get; set; }
        [XmlElement(ElementName = "HexSettingID")]
        public string HexSettingId { get; set; }
        public string Description { get; set; }
        public string GroupName { get; set; }
        public string OverrideDefault { get; set; }
        public float MinRequiredDriverVersion { get; set; }
        public bool Hidden { get; set; }
        public bool HasConstraints { get; set; }
        public string DataType { get; set; }

        public List<CustomSettingValue> SettingValues { get; set; } = new List<CustomSettingValue>();

        private ulong? _cachedDefaultValue;
        private ulong _cachedSettingId;

        internal ulong SettingId
        {
            get
            {
                if (_cachedSettingId == 0 && !string.IsNullOrEmpty(HexSettingId))
                {
                    _cachedSettingId = ConvertHexToUInt64(HexSettingId);
                }
                return _cachedSettingId;
            }
        }

        internal ulong? DefaultValue
        {
            get
            {
                if (_cachedDefaultValue == null && !string.IsNullOrEmpty(OverrideDefault))
                {
                    _cachedDefaultValue = ConvertHexToUInt64(OverrideDefault);
                }
                return _cachedDefaultValue;
            }
        }

        private ulong ConvertHexToUInt64(string hexValue)
        {
            string strippedValue = StripAndValidateHex(hexValue.Trim());
            return Convert.ToUInt64(strippedValue, 16);
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
using System;
using System.Collections.Generic;

namespace CommSdk.Transports.Factories
{
    internal static class TransportConfigParser
    {
        public static string GetString(IDictionary<string, string> parameters, string key, string defaultValue = null)
        {
            if (parameters == null || key == null) return defaultValue;
            string value;
            if (parameters.TryGetValue(key, out value)) return value;
            foreach (var pair in parameters)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)) return pair.Value;
            }
            return defaultValue;
        }

        public static int GetInt(IDictionary<string, string> parameters, string key, int defaultValue)
        {
            var raw = GetString(parameters, key, null);
            int value;
            return int.TryParse(raw, out value) ? value : defaultValue;
        }

        public static TEnum GetEnum<TEnum>(IDictionary<string, string> parameters, string key, TEnum defaultValue)
        {
            var raw = GetString(parameters, key, null);
            if (string.IsNullOrEmpty(raw)) return defaultValue;
            try
            {
                return (TEnum)Enum.Parse(typeof(TEnum), raw, true);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}

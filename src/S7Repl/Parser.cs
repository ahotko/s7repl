using System;
using System.Collections.Generic;
using System.Linq;

namespace S7ReplApp
{
    internal static class Parser
    {
        public class Parameter
        {
        }

        public class Parameter<T> : Parameter
        {
            public T Value { get; init; }

            public Parameter(T value)
            {
                Value = value;
            }
        }

        private static T GetValue<T>(string[] values, int index, string name, T defaultValue, bool optional = true)
        {
            if (index >= values.Length)
            {
                if (optional)
                    return defaultValue;

                throw new ArgumentNullException(name, $"Missing value for '{name}'");
            }

            string result = values[index] ?? string.Empty;

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public static (string IpAddress, int Rack, int Slot) ParseConnectParameters(string[] parameters)
        {
            return (GetValue(parameters, 0, "IP", "", false),
            GetValue<int>(parameters, 1, "rack", 0),
            GetValue<int>(parameters, 2, "slot", 0));
        }

        public static (int DataBlock, int Offset, int Length, bool UseDecimal) ParseDumpParameters(string[] parameters)
        {
            var useDecimal = GetValue<string>(parameters, 3, "Use Decimal Values", "hex");

            return (GetValue<int>(parameters, 0, "Data Block", 0, false),
            GetValue<int>(parameters, 1, "Offset", 0, false),
            GetValue<int>(parameters, 2, "Length", 0, false),
            useDecimal.Equals("dec", StringComparison.OrdinalIgnoreCase));
        }

        public static (string IpAddress, int Rack, int Slot) ParseReadParameters(string[] parameters)
        {
            return (GetValue(parameters, 0, "IP", "", false),
            GetValue<int>(parameters, 1, "rack", 0),
            GetValue<int>(parameters, 2, "slot", 0));
        }

        public static (string Command, string[] Params) ParseCommand(string line)
        {
            var parts = line.Trim().Split();

            var command = parts[0].ToLower();

            return (command, parts.Skip(1).ToArray());
        }

    }
}

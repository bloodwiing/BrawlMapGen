using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BMG
{
    class Utils
    {
        public static string StringVariables(string original, Dictionary<string, object> replacements)
        {
            return Regex.Replace(
                original,
                @"(?<!\{)\{([^}]+)\}(?!\})",
                match => replacements.TryGetValue(match.Groups[1].Value, out var value) ? value.ToString() : "???");
        }
    }
}

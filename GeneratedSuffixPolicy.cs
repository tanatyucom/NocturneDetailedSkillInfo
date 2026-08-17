using System;
using System.Collections.Generic;

namespace NocturneDetailedSkillInfo
{
    internal static class GeneratedSuffixPolicy
    {
        internal static string RemoveKnownSuffix(
            string incoming,
            IEnumerable<string>? knownSuffixes)
        {
            string text = incoming ?? "";

            if (knownSuffixes == null)
                return text;

            string? longestMatch = null;

            foreach (string suffix in knownSuffixes)
            {
                if (String.IsNullOrEmpty(suffix))
                    continue;

                if (!text.EndsWith(suffix, StringComparison.Ordinal))
                    continue;

                if (longestMatch == null || suffix.Length > longestMatch.Length)
                    longestMatch = suffix;
            }

            return longestMatch == null
                ? text
                : text.Substring(0, text.Length - longestMatch.Length);
        }

        internal static string Append(string original, string suffix)
        {
            string text = original ?? "";
            return String.IsNullOrEmpty(suffix) ? text : text + suffix;
        }
    }
}

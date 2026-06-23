namespace Launcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Resolves changelog lines from manifest / release-notes.txt.
    ///     Legacy bilingual lines (<c>English || German</c>) still strip to the English half only.
    /// </summary>
    internal static class ChangelogLocalization
    {
        private static readonly Regex BilingualSeparator =
            new(@"\s*\|\|\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal static bool LooksBilingual(string line) =>
            !string.IsNullOrWhiteSpace(line) && BilingualSeparator.IsMatch(line.Trim());

        internal static string ResolveLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            return EnglishHalf(line.Trim());
        }

        internal static IReadOnlyList<string> ResolveLines(IEnumerable<string> lines) =>
            lines.Select(ResolveLine).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        internal static string EnglishHalf(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            var trimmed = line.Trim();
            var parts = BilingualSeparator.Split(trimmed, 2);
            return parts.Length < 2 ? trimmed : parts[0].Trim();
        }
    }
}

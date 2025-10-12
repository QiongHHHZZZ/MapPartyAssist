using System.Text.RegularExpressions;

namespace MapPartyAssist.Helper {
    internal static class RegexHelper {
        internal static string GetGroupValue(Match match, string? groupName = null) {
            if(!match.Success) {
                return string.Empty;
            }

            if(!string.IsNullOrEmpty(groupName)) {
                var group = match.Groups[groupName];
                if(group.Success) {
                    return group.Value;
                }
            }

            return match.Value;
        }

        internal static string SanitizeQuotedText(string text) {
            if(string.IsNullOrEmpty(text)) {
                return text;
            }

            return text.Trim('“', '”', '「', '」', '『', '』', '"', ' ');
        }

        internal static string EnsureQuantityText(string candidate, string sourceText) {
            if(!string.IsNullOrEmpty(candidate) && Regex.IsMatch(candidate, @"\d+")) {
                return candidate;
            }

            var multiplyMatch = Regex.Match(sourceText, @"×\s*([\d,\.]+)");
            if(multiplyMatch.Success) {
                return multiplyMatch.Groups[1].Value;
            }

            return candidate;
        }
    }
}

using System.Collections.Generic;
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

            var trimmed = text.Trim('“', '”', '「', '」', '『', '』', '"', ' ');
            return Regex.Replace(trimmed, "[\\uE000-\\uF8FF]", string.Empty);
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

        internal static bool TryParseChineseNumber(string text, out int value) {
            value = 0;
            if(string.IsNullOrWhiteSpace(text)) {
                return false;
            }

            text = text.Replace(",", string.Empty)
                       .Replace(".", string.Empty)
                       .Replace(" ", string.Empty)
                       .Replace("，", string.Empty)
                       .Replace("．", string.Empty);

            if(int.TryParse(text, out value)) {
                return true;
            }

            var digitMap = new Dictionary<char, int> {
                { '零', 0 }, { '〇', 0 }, { '一', 1 }, { '二', 2 }, { '三', 3 }, { '四', 4 },
                { '五', 5 }, { '六', 6 }, { '七', 7 }, { '八', 8 }, { '九', 9 }
            };
            var unitMap = new Dictionary<char, int> {
                { '十', 10 }, { '百', 100 }, { '千', 1000 }, { '万', 10000 }, { '億', 100000000 },
                { '亿', 100000000 }
            };

            int total = 0;
            int section = 0;
            int number = 0;
            int digitBuffer = -1;
            bool parsedAny = false;

            void FlushDigitBuffer() {
                if(digitBuffer >= 0) {
                    number = digitBuffer;
                    digitBuffer = -1;
                    parsedAny = true;
                }
            }

            foreach(char ch in text) {
                if(digitMap.TryGetValue(ch, out var digit)) {
                    FlushDigitBuffer();
                    number = digit;
                    parsedAny = true;
                } else if(char.IsDigit(ch)) {
                    if(digitBuffer < 0) {
                        digitBuffer = 0;
                    }
                    digitBuffer = digitBuffer * 10 + (ch - '0');
                    parsedAny = true;
                } else if(ch is '十' or '百' or '千') {
                    FlushDigitBuffer();
                    if(number == 0) {
                        number = 1;
                    }
                    section += number * unitMap[ch];
                    number = 0;
                } else if(ch is '万' or '億' or '亿') {
                    FlushDigitBuffer();
                    if(number != 0) {
                        section += number;
                    }
                    if(section == 0) {
                        section = 1;
                    }
                    total += section * unitMap[ch];
                    section = 0;
                    number = 0;
                    parsedAny = true;
                }
            }

            FlushDigitBuffer();
            section += number;
            total += section;
            value = total;
            return parsedAny;
        }
    }
}

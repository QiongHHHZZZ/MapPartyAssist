using MapPartyAssist.Types;
using System;
using System.Collections.Generic;

namespace MapPartyAssist.Helper {
    internal static class PlayerHelper {
        private static readonly HashSet<string> ChinaDataCenters = new(StringComparer.OrdinalIgnoreCase) {
            "陆行鸟",
            "莫古力",
            "猫小胖",
            "豆豆柴",
            "中国",
        };

        private static readonly HashSet<string> KoreaDataCenters = new(StringComparer.OrdinalIgnoreCase) {
            "한국",
            "韓國",
            "韩国",
            "Korea",
        };

        public static Region GetRegion(byte? regionByte, string? dataCenterName = null) {
            if(regionByte.HasValue) {
                switch(regionByte.Value) {
                    case 1:
                        return Region.Japan;
                    case 2:
                        return Region.NorthAmerica;
                    case 3:
                        return Region.Europe;
                    case 4:
                        return Region.Oceania;
                    case 5:
                        return Region.China;
                    case 6:
                        return Region.Korea;
                }
            }

            return MapByDataCenterName(dataCenterName);
        }

        private static Region MapByDataCenterName(string? dataCenterName) {
            if(string.IsNullOrWhiteSpace(dataCenterName)) {
                return Region.Unknown;
            }

            string normalized = dataCenterName.Trim();
            if(ChinaDataCenters.Contains(normalized)) {
                return Region.China;
            }

            if(KoreaDataCenters.Contains(normalized)) {
                return Region.Korea;
            }

            return Region.Unknown;
        }

        public static bool IsAliasMatch(string fullName, string abbreviatedName) {
            if(string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(abbreviatedName)) {
                return false;
            }

            var abbreviatedNameList = abbreviatedName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fullNameList = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if(abbreviatedNameList.Length >= 2 && fullNameList.Length >= 2) {
                for(int i = 0; i < 2; i++) {
                    // guard in case full name does not have both parts
                    if(i >= fullNameList.Length || i >= abbreviatedNameList.Length) {
                        break;
                    }
                    var curFullName = fullNameList[i];
                    var curAbbreviatedName = abbreviatedNameList[i];
                    if(curAbbreviatedName.Contains('.')) {
                        if(curFullName.Length == 0 || curFullName[0] != curAbbreviatedName[0]) {
                            return false;
                        }
                    } else if(!curFullName.Equals(curAbbreviatedName, StringComparison.OrdinalIgnoreCase)) {
                        return false;
                    }
                }
                return true;
            }

            // Fallback for locales that do not include spaces or abbreviations (e.g. Chinese/Korean).
            var normalizedFull = NormalizeAlias(fullName);
            var normalizedAbbrev = NormalizeAlias(abbreviatedName);
            if(normalizedFull.Length == 0 || normalizedAbbrev.Length == 0) {
                return false;
            }

            if(normalizedFull.Equals(normalizedAbbrev, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            // Allow alias without the home world or abbreviated form that still uniquely prefixes the full key.
            if(normalizedFull.StartsWith(normalizedAbbrev, StringComparison.OrdinalIgnoreCase)
                || normalizedAbbrev.StartsWith(normalizedFull, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            return false;
        }

        private static string NormalizeAlias(string value) {
            Span<char> buffer = value.Length <= 64 ? stackalloc char[value.Length] : value.ToCharArray();
            int index = 0;
            foreach(var ch in value) {
                if(char.IsWhiteSpace(ch)
                    || ch == '.'
                    || ch == '“'
                    || ch == '”'
                    || ch == '「'
                    || ch == '」'
                    || ch == '『'
                    || ch == '』'
                    || ch == '"') {
                    continue;
                }

                buffer[index++] = char.ToLowerInvariant(ch);
            }

            return new string(buffer[..index]);
        }
    }
}

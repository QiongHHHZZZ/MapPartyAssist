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
            var abbreviatedNameList = abbreviatedName.Trim().Split(' ');
            var fullNameList = fullName.Trim().Split(' ');
            if(abbreviatedNameList.Length < 2) {
                return false;
            }
            for(int i = 0; i < 2; i++) {
                var curFullName = fullNameList[i];
                var curAbbreviatedName = abbreviatedNameList[i];
                if(curAbbreviatedName.Contains('.')) {
                    if(curFullName[0] != curAbbreviatedName[0]) {
                        return false;
                    }
                } else if(!curFullName.Equals(curAbbreviatedName, StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }
            return true;
        }
    }
}

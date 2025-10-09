using System;
using System.Globalization;

namespace MapPartyAssist.Helper {
    public static class StringHelper {
        public static string AddOrdinal(int num) {
            if(num <= 0) {
                return num.ToString(CultureInfo.CurrentCulture);
            }

            var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var currentUICulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if(string.Equals(currentCulture, "zh", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(currentUICulture, "zh", StringComparison.OrdinalIgnoreCase)) {
                return $"第{num}";
            }

            switch(num % 100) {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            return (num % 10) switch {
                1 => num + "st",
                2 => num + "nd",
                3 => num + "rd",
                _ => num + "th",
            };
        }
    }
}

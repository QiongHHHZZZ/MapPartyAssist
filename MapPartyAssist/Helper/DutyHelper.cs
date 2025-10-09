using MapPartyAssist.Types;
using MapPartyAssist.Localization;

namespace MapPartyAssist.Helper {
    internal static class DutyHelper {

        public static string GetSummonName(Summon summon) {
            return summon switch {
                Summon.Lesser => Loc.Tr("Lesser"),
                Summon.Greater => Loc.Tr("Greater"),
                Summon.Elder => Loc.Tr("Elder"),
                Summon.Gold => Loc.Tr("Circle Shift"),
                Summon.Silver => Loc.Tr("Abomination"),
                _ => Loc.Tr("Unknown"),
            };
        }

    }
}

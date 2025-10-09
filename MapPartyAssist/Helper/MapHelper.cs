using MapPartyAssist.Localization;
using MapPartyAssist.Types;
using System.Collections.Generic;

namespace MapPartyAssist.Helper {
    internal static class MapHelper {

        public static Dictionary<uint, TreasureMap> IdToMapTypeMap = new() {
            {2000297,  TreasureMap.Leather},
            {2001088,  TreasureMap.Goatskin},
            {2001089,  TreasureMap.Toadskin},
            {2001090,  TreasureMap.Boarskin},
            {2001091,  TreasureMap.Peisteskin},
            {2001223,  TreasureMap.Alexandrite},
            {2001352,  TreasureMap.Unhidden},
            {2001762,  TreasureMap.Archaeoskin},
            {2001763,  TreasureMap.Wyvernskin},
            {2001764,  TreasureMap.Dragonskin},
            {2002209,  TreasureMap.Gaganaskin},
            {2002210,  TreasureMap.Gazelleskin},
            {2002260,  TreasureMap.Thief},
            {2002503,  TreasureMap.SeeminglySpecial},
            {2002663,  TreasureMap.Gliderskin},
            {2002664,  TreasureMap.Zonureskin},
            {2003075,  TreasureMap.OstensiblySpecial},
            {2003245,  TreasureMap.Saigaskin},
            {2003246,  TreasureMap.Kumbhiraskin},
            {2003455,  TreasureMap.PotentiallySpecial},
            {2003457,  TreasureMap.Ophiotauroskin},
            {2003463,  TreasureMap.ConceivablySpecial},
            {2003562,  TreasureMap.Loboskin},
            {2003563,  TreasureMap.Braaxskin},
            {2003785,  TreasureMap.Gargantuaskin},
        };

        public static string GetMapName(TreasureMap map) {
            return map switch {
                TreasureMap.Leather => Loc.Tr("Leather Treasure Map"),
                TreasureMap.Goatskin => Loc.Tr("Goatskin Treasure Map"),
                TreasureMap.Toadskin => Loc.Tr("Toadskin Treasure Map"),
                TreasureMap.Boarskin => Loc.Tr("Boarskin Treasure Map"),
                TreasureMap.Peisteskin => Loc.Tr("Peisteskin Treasure Map"),
                TreasureMap.Alexandrite => Loc.Tr("Alexandrite Treasure Map"),
                TreasureMap.Unhidden => Loc.Tr("Leather Buried Treasure Map"),
                TreasureMap.Archaeoskin => Loc.Tr("Archaeoskin Treasure Map"),
                TreasureMap.Wyvernskin => Loc.Tr("Wyvernskin Treasure Map"),
                TreasureMap.Dragonskin => Loc.Tr("Dragonskin Treasure Map"),
                TreasureMap.Gaganaskin => Loc.Tr("Gaganaskin Treasure Map"),
                TreasureMap.Gazelleskin => Loc.Tr("Gazelleskin Treasure Map"),
                TreasureMap.Thief => Loc.Tr("Fabled Thief's Map"),
                TreasureMap.SeeminglySpecial => Loc.Tr("Seemingly Special Treasure Map"),
                TreasureMap.Gliderskin => Loc.Tr("Gliderskin Treasure Map"),
                TreasureMap.Zonureskin => Loc.Tr("Zonureskin Treasure Map"),
                TreasureMap.OstensiblySpecial => Loc.Tr("Ostensibly Special Treasure Map"),
                TreasureMap.Saigaskin => Loc.Tr("Saigaskin Treasure Map"),
                TreasureMap.Kumbhiraskin => Loc.Tr("Kumbhiraskin Treasure Map"),
                TreasureMap.Ophiotauroskin => Loc.Tr("Ophiotauroskin Treasure Map"),
                TreasureMap.PotentiallySpecial => Loc.Tr("Potentially Special Treasure Map"),
                TreasureMap.ConceivablySpecial => Loc.Tr("Conceivably Special Treasure Map"),
                TreasureMap.Loboskin => Loc.Tr("Loboskin Treasure Map"),
                TreasureMap.Braaxskin => Loc.Tr("Br'aaxskin Treasure Map"),
                TreasureMap.Gargantuaskin => Loc.Tr("Gargantuaskin Treasure Map"),
                _ => Loc.Tr("Unknown")
            };
        }

        public static string GetCategoryName(TreasureMapCategory category) {
            return category switch {
                TreasureMapCategory.ARealmReborn => Loc.Tr("A Realm Reborn"),
                TreasureMapCategory.Heavensward => Loc.Tr("Heavensward"),
                TreasureMapCategory.Stormblood => Loc.Tr("Stormblood"),
                TreasureMapCategory.Shadowbringers => Loc.Tr("Shadowbringers"),
                TreasureMapCategory.Endwalker => Loc.Tr("Endwalker"),
                TreasureMapCategory.Elpis => Loc.Tr("Elpis"),
                TreasureMapCategory.Dawntrail => Loc.Tr("Dawntrail"),
                TreasureMapCategory.LivingMemory => Loc.Tr("Living Memory"),
                TreasureMapCategory.Unknown => Loc.Tr("Unknown/Unrecorded"),
                _ => Loc.Tr("Unknown/Unrecorded"),
            };
        }

        public static TreasureMapCategory GetCategory(int territoryId) {
            if(territoryId == 0) {
                return TreasureMapCategory.Unknown;
            } else if(territoryId < 397) {
                return TreasureMapCategory.ARealmReborn;
            } else if(territoryId < 612) {
                return TreasureMapCategory.Heavensward;
            } else if(territoryId < 812) {
                return TreasureMapCategory.Stormblood;
            } else if(territoryId < 956) {
                return TreasureMapCategory.Shadowbringers;
            } else if(territoryId == 961) {
                return TreasureMapCategory.Elpis;
            } else if(territoryId <= 1185) {
                return TreasureMapCategory.Endwalker;
            } else if(territoryId == 1192) {
                return TreasureMapCategory.LivingMemory;
            } else {
                return TreasureMapCategory.Dawntrail;
            }
        }
    }
}

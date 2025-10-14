using Dalamud.Game;
using LiteDB;
using MapPartyAssist.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapPartyAssist.Types {
    public class Duty(
        int id,
        string name,
        DutyStructure structure,
        int chamberCount,
        List<Checkpoint>? checkpoints = null,
        Checkpoint? failureCheckpoint = null,
        string[]? lesserSummons = null,
        string[]? greaterSummons = null,
        string[]? elderSummons = null,
        string[]? finalSummons = null)
    {
        public int DutyId { get; } = id;
        public string Name { get; } = name;
        public DutyStructure Structure { get; } = structure;
        public int ChamberCount { get; } = chamberCount;
        public Type? ResultsType { get; init; }
        public List<Checkpoint>? Checkpoints { get; } = checkpoints;
        public uint? TerritoryTypeId { get; set; }
        public Checkpoint? FailureCheckpoint { get; set; } = failureCheckpoint;
        public string[]? LesserSummons { get; } = lesserSummons;
        public string[]? GreaterSummons { get; } = greaterSummons;
        public string[]? ElderSummons { get; } = elderSummons;
        public string[]? FinalSummons { get; } = finalSummons;

        [BsonIgnore]
        public Dictionary<ClientLanguage, Regex>? LesserSummonRegex { get; set; }
        [BsonIgnore]
        public Dictionary<ClientLanguage, Regex>? GreaterSummonRegex { get; set; }
        [BsonIgnore]
        public Dictionary<ClientLanguage, Regex>? ElderSummonRegex { get; set; }
        [BsonIgnore]
        public Dictionary<ClientLanguage, Regex>? CircleShiftsRegex { get; set; }

        public string GetSummonPatternString(Summon summonType) {
            List<string> summonList;
            switch(summonType) {
                case Summon.Lesser:
                    if(LesserSummons == null) {
                        throw new InvalidOperationException("Duty missing lesser summons");
                    }
                    summonList = LesserSummons.ToList();
                    break;
                case Summon.Greater:
                    if(GreaterSummons == null) {
                        throw new InvalidOperationException("Duty missing greater summons");
                    }
                    summonList = GreaterSummons.ToList();
                    break;
                case Summon.Elder:
                    if(ElderSummons == null) {
                        throw new InvalidOperationException("Duty missing elder summons");
                    }
                    summonList = ElderSummons.ToList();
                    if(FinalSummons != null) {
                        summonList = summonList.Concat(FinalSummons).ToList();
                    }
                    break;
                default:
                    return "";
            }

            string pattern = "(";
            for(int i = 0; i < summonList.Count; i++) {
                pattern += summonList[i];
                if(i == summonList.Count - 1) {
                    pattern += ")";
                } else {
                    pattern += "|";
                }
            }
            return pattern;
        }

        public string GetDisplayName() {
            string displayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Name);
            //re-lowercase 'of'
            displayName = Regex.Replace(displayName, @"(?<!^)\bof\b", "of", RegexOptions.IgnoreCase);
            return Loc.Tr(displayName);
        }
    }

    public enum DutyStructure {
        Doors,
        Roulette,
        Slots
    }
}

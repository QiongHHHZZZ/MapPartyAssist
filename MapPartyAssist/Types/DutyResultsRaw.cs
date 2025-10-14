using Dalamud.Game;
using LiteDB;
using System;
using System.Collections.Generic;

namespace MapPartyAssist.Types {
    internal class DutyResultsRaw {

        [BsonId]
        public ObjectId Id { get; set; } = new();
        public DateTime Time { get; set; } = DateTime.Now;
        public int DutyId { get; init; }
        public bool IsComplete { get; set; }
        public bool IsParsed { get; set; }
        public ClientLanguage Language { get; set; }
        public List<Message> Messages { get; set; } = new();
        [BsonRef("map")]
        public MPAMap? Map { get; set; }
        public string? Owner { get; set; }
        public string[] Players { get; set; } = [];
    }
}

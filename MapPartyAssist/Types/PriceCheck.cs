using LiteDB;
using System;

namespace MapPartyAssist.Types {
    internal class PriceCheck {
        [BsonId]
        public ObjectId Id { get; set; } = new ObjectId();
        public uint ItemId { get; init; }
        public uint NQPrice { get; set; }
        public uint HQPrice { get; set; }
        public DateTime LastChecked { get; set; }
        public Region Region { get; set; }
    }
}

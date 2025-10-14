using System;

namespace MapPartyAssist.Types {
    public class Message(DateTime time, int channel, string text, uint? itemId = null, bool? isHq = null, string? playerKey = null) {

        public DateTime Time { get; set; } = time;
        public int Channel { get; set; } = channel;
        public string Text { get; set; } = text;
        public uint? ItemId { get; set; } = itemId;
        public bool? IsHq { get; set; } = isHq;
        public string? PlayerKey { get; set; } = playerKey;
    }
}

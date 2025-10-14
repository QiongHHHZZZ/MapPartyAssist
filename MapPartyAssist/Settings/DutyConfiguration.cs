namespace MapPartyAssist.Settings {
    public class DutyConfiguration {
        public int DutyId { get; set; }
        public bool DisplayClearSequence { get; set; }
        public bool DisplayRunsSinceLastClear { get; set; } = true;
        public bool DisplayDeaths { get; set; }
        public bool OmitZeroCheckpoints { get; set; }

        public DutyConfiguration() {
        }

        public DutyConfiguration(int dutyId, bool displayClearSequence = false, bool displayDeaths = false) {
            DutyId = dutyId;
            DisplayClearSequence = displayClearSequence;
            DisplayDeaths = displayDeaths;
        }
    }
}

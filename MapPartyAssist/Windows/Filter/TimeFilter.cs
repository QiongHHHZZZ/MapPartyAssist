using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using MapPartyAssist.Localization;
using System;

namespace MapPartyAssist.Windows.Filter {

    public enum StatRange {
        Current,
        PastDay,
        PastWeek,
        ThisMonth,
        LastMonth,
        ThisYear,
        LastYear,
        SinceLastClear,
        All,
        Custom
    }

    public class TimeFilter : DataFilter {

        public override string Name => Loc.Tr("Time");
        public override string HelpMessage => Loc.Tr("'Current' limits to maps and linked duties on the map tracker.\nCustom time ranges input auto-formats using your local timezone.");

        public StatRange StatRange { get; set; } = StatRange.All;
        public static string[] Range => new[] { Loc.Tr("Current"), Loc.Tr("Past 24 hours"), Loc.Tr("Past 7 days"), Loc.Tr("This month"), Loc.Tr("Last month"), Loc.Tr("This year"), Loc.Tr("Last year"), Loc.Tr("Since last clear"), Loc.Tr("All-time"), Loc.Tr("Custom") };

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        private string _lastStartTime = "";
        private string _lastEndTime = "";

        public TimeFilter() { }

        internal TimeFilter(Plugin plugin, Action action, TimeFilter? filter = null) : base(plugin, action) {
            if(filter is not null) {
                StatRange = filter.StatRange;
                StartTime = filter.StartTime;
                EndTime = filter.EndTime;
            }
        }

        internal override void Draw() {
            int statRangeToInt = (int)StatRange;
            var rangeOptions = Range;
            ImGui.SetNextItemWidth(float.Max(ImGui.GetContentRegionAvail().X / 2f, ImGuiHelpers.GlobalScale * 100f));
            if(ImGui.Combo($"##timeRangeCombo", ref statRangeToInt, rangeOptions, rangeOptions.Length)) {
                _plugin!.DataQueue.QueueDataOperation(() => {
                    StatRange = (StatRange)statRangeToInt;
                    Refresh();
                });
            }
            if(StatRange == StatRange.Custom) {
                using var table = ImRaii.Table("timeFilterTable", 2);
                if(table) {
                    ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(Loc.Tr("Start:"));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                    var startTime = StartTime.ToString();
                    if(ImGui.InputText($"##startTime", ref startTime, 50, ImGuiInputTextFlags.None)) {
                        if(startTime != _lastStartTime) {
                            _lastStartTime = startTime;
                            if(DateTime.TryParse(startTime, out DateTime newStartTime)) {
                                _plugin!.DataQueue.QueueDataOperation(() => {
                                    StartTime = newStartTime;
                                    Refresh();
                                });
                            }
                        }
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(Loc.Tr("End:"));
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    var endTime = EndTime.ToString();
                    if(ImGui.InputText($"##endTime", ref endTime, 50, ImGuiInputTextFlags.None)) {
                        if(endTime != _lastEndTime) {
                            _lastEndTime = endTime;
                            if(DateTime.TryParse(endTime, out DateTime newEndTime)) {
                                _plugin!.DataQueue.QueueDataOperation(() => {
                                    EndTime = newEndTime;
                                    Refresh();
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}

using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using MapPartyAssist.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapPartyAssist.Windows.Filter {
    public class DutyFilter : DataFilter {

        public override string Name => Loc.Tr("Duty");

        public Dictionary<int, bool> FilterState { get; private set; } = new();
        internal bool AllSelected { get; private set; } = false;
        private readonly Dictionary<string, List<int>> _dutyGroups = new(StringComparer.Ordinal);
        private readonly List<string> _sortedGroupNames = new();

        internal DutyFilter() { }

        internal DutyFilter(Plugin plugin, Action action, DutyFilter? filter = null) : base(plugin, action) {
            _plugin = plugin;
            foreach(var duty in _plugin.DutyManager.Duties) {
                FilterState!.Add(duty.Key, true);
                if(filter is not null && filter.FilterState.ContainsKey(duty.Key)) {
                    FilterState[duty.Key] = filter.FilterState[duty.Key];
                }

                string displayName = duty.Value.GetDisplayName();
                if(!_dutyGroups.TryGetValue(displayName, out var dutyIds)) {
                    dutyIds = new List<int>();
                    _dutyGroups.Add(displayName, dutyIds);
                }
                dutyIds.Add(duty.Key);
            }

            _sortedGroupNames.AddRange(_dutyGroups.Keys);
            _sortedGroupNames.Sort(StringComparer.CurrentCultureIgnoreCase);
            UpdateAllSelected();
        }

        private void UpdateAllSelected() {
            AllSelected = FilterState!.Values.All(state => state);
        }

        internal override void Draw() {
            bool allSelected = AllSelected;
            if(ImGui.Checkbox(Loc.Tr("Select All") + $"##{GetHashCode()}", ref allSelected)) {
                _plugin!.DataQueue.QueueDataOperation(() => {
                    foreach(var duty in FilterState) {
                        FilterState![duty.Key] = allSelected;
                    }
                    AllSelected = allSelected;
                    Refresh();
                });
            }

            using var table = ImRaii.Table("dutyFilterTable", 2);
            if(table) {
                ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 2, ImGuiHelpers.GlobalScale * 400f));
                ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 2, ImGuiHelpers.GlobalScale * 400f));
                ImGui.TableNextRow();

#pragma warning disable CS8602
                for(int index = 0; index < _sortedGroupNames.Count; index++) {
                    string dutyName = _sortedGroupNames[index];
                    if(!_dutyGroups.TryGetValue(dutyName, out var dutyIds) || dutyIds == null || dutyIds.Count == 0) {
                        continue;
                    }
                    var dutyIdsSnapshot = dutyIds.ToArray();
                    var filterStateDict = FilterState ?? throw new InvalidOperationException("FilterState not initialized.");
                    bool filterState = dutyIdsSnapshot.All(id => filterStateDict.TryGetValue(id, out var state) && state);
                    ImGui.TableNextColumn();
                    if(ImGui.Checkbox($"{dutyName}##{GetHashCode()}_{index}", ref filterState)) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            var dict = FilterState ?? throw new InvalidOperationException("FilterState not initialized.");
                            foreach(var dutyId in dutyIdsSnapshot) {
                                dict[dutyId] = filterState;
                            }
                            UpdateAllSelected();
                            Refresh();
                        });
                    }
                }
#pragma warning restore CS8602
            }
        }
    }
}


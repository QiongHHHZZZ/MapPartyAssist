using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using MapPartyAssist.Localization;
using System;

namespace MapPartyAssist.Windows.Filter {
    public class MiscFilter : DataFilter {
        public override string Name => Loc.Tr("Other");

        public bool LootOnly { get; set; }

        public bool ShowDeleted { get; set; }

        public bool SelfLootOnly { get; set; }

        public MiscFilter() { }

        internal MiscFilter(Plugin plugin, Action action, MiscFilter? filter = null) : base(plugin, action) {
            if(filter is not null) {
                LootOnly = filter.LootOnly;
                ShowDeleted = filter.ShowDeleted;
                SelfLootOnly = filter.SelfLootOnly;
            }
        }

        internal override void Draw() {
            using var table = ImRaii.Table("miscFilterTable", 3, ImGuiTableFlags.NoClip);
            if(table) {
                ImGui.TableSetupColumn($"c1", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 350f));
                ImGui.TableSetupColumn($"c2", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 350f));
                ImGui.TableSetupColumn($"c3", ImGuiTableColumnFlags.WidthFixed, float.Min(ImGui.GetContentRegionAvail().X / 3, ImGuiHelpers.GlobalScale * 350f));
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                bool lootOnly = LootOnly;
                if(ImGui.Checkbox(Loc.Tr("Must have loot"), ref lootOnly)) {
                    _plugin!.DataQueue.QueueDataOperation(() => {
                        LootOnly = lootOnly;
                        Refresh();
                    });
                }
                ImGui.TableNextColumn();
                bool showDeleted = ShowDeleted;
                if(ImGui.Checkbox(Loc.Tr("Show deleted/incomplete"), ref showDeleted)) {
                    _plugin!.DataQueue.QueueDataOperation(() => {
                        ShowDeleted = showDeleted;
                        Refresh();
                    });
                }
                ImGui.TableNextColumn();
                bool selfLootOnly = SelfLootOnly;
                if(ImGui.Checkbox(Loc.Tr("Only show self-obtained loot"), ref selfLootOnly)) {
                    _plugin!.DataQueue.QueueDataOperation(() => {
                        SelfLootOnly = selfLootOnly;
                        Refresh();
                    });
                }
            }
        }
    }
}


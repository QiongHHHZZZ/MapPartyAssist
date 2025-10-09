using Dalamud.Bindings.ImGui;
using MapPartyAssist.Localization;
using System;

namespace MapPartyAssist.Windows.Filter {
    public class ProgressFilter : DataFilter {
        public override string Name => Loc.Tr("Progress");

        public bool OnlyClears { get; set; }

        public ProgressFilter() { }

        internal ProgressFilter(Plugin plugin, Action action, ProgressFilter? filter = null) : base(plugin, action) {
            if(filter is not null) {
                OnlyClears = filter.OnlyClears;
            }
        }

        internal override void Draw() {
            bool onlyClears = OnlyClears;
            if(ImGui.Checkbox(Loc.Tr("Full clears only"), ref onlyClears)) {
                _plugin!.DataQueue.QueueDataOperation(() => {
                    OnlyClears = onlyClears;
                    Refresh();
                });
            }
        }
    }
}


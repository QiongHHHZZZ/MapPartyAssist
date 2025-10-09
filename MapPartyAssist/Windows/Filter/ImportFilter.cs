using Dalamud.Bindings.ImGui;
using MapPartyAssist.Localization;
using System;

namespace MapPartyAssist.Windows.Filter {
    public class ImportFilter : DataFilter {
        public override string Name => Loc.Tr("Imports");

        public override string HelpMessage => Loc.Tr("Checking this can reduce the amount of information in 'Duty Progress Summary' \ndepending on what was recorded.");

        public bool IncludeImports { get; set; }

        public ImportFilter() { }

        internal ImportFilter(Plugin plugin, Action action, ImportFilter? filter = null) : base(plugin, action) {
            if(filter is not null) {
                IncludeImports = filter.IncludeImports;
            }
        }

        internal override void Draw() {
            bool includeImports = IncludeImports;
            if(ImGui.Checkbox(Loc.Tr("Include imported duty stats"), ref includeImports)) {
                _plugin!.DataQueue.QueueDataOperation(() => {
                    IncludeImports = includeImports;
                    Refresh();
                });
            }
        }
    }
}





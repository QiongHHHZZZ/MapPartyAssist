using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using LiteDB;
using Lumina.Excel.Sheets;
using MapPartyAssist.Helper;
using MapPartyAssist.Localization;
using MapPartyAssist.Types;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static Dalamud.Interface.Windowing.Window;

namespace MapPartyAssist.Windows.Summary {
    internal class DutyResultsListView {

        private const int _maxPageSize = 100;

        private Plugin _plugin;
        private StatsWindow _statsWindow;
        private List<DutyResults> _dutyResults = new();
        private List<DutyResults> _dutyResultsPage = new();
        private Dictionary<ObjectId, Dictionary<LootResultKey, LootResultValue>> _lootResults = new();
        private Dictionary<ObjectId, int> _totalGilValue = new();

        private int _currentPage = 0;
        private bool _collapseAll = false;
        private SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        public string CSV { get; private set; } = "";

        internal DutyResultsListView(Plugin plugin, StatsWindow statsWindow) {
            _plugin = plugin;
            _statsWindow = statsWindow;
        }

        public void Refresh(List<DutyResults> dutyResults) {
            Dictionary<ObjectId, Dictionary<LootResultKey, LootResultValue>> lootResults = new();
            Dictionary<ObjectId, int> totalGilValue = new();
            //calculate loot results (this is largely duplicated from lootsummary)
            List<string> selfPlayers = new();
            _plugin.StorageManager.GetPlayers().Query().Where(p => p.IsSelf).ToList().ForEach(p => {
                selfPlayers.Add(p.Key);
            });

            var itemSheet = _plugin.DataManager.GetExcelSheet<Item>();
            foreach(var dr in dutyResults) {
                if(!dr.HasLootResults()) {
                    continue;
                }
                Dictionary<LootResultKey, LootResultValue> newLootResults = new();
                int newTotalGil = 0;
                foreach(var checkpointResult in dr.CheckpointResults) {
                    if(checkpointResult.LootResults == null) continue;
                    foreach(var lootResult in checkpointResult.LootResults.Where(x => x.Recipient != null)) {
                        bool selfObtained = lootResult.Recipient is not null && selfPlayers.Contains(lootResult.Recipient);
                        newTotalGil += LootAggregationHelper.Accumulate(
                            newLootResults,
                            lootResult,
                            selfObtained,
                            dr.Players.Length,
                            itemId => itemSheet?.GetRow(itemId),
                            key => _plugin.PriceHistory.CheckPrice(key));
                    }
                }
                newLootResults = newLootResults.OrderByDescending(lr => lr.Value.DroppedQuantity).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                lootResults.Add(dr.Id, newLootResults);
                totalGilValue.Add(dr.Id, newTotalGil);
            }
            try {
                _refreshLock.Wait();
                _dutyResults = dutyResults;
                _lootResults = lootResults;
                _totalGilValue = totalGilValue;
            } finally {
                _refreshLock.Release();
            }
            if(_currentPage * _maxPageSize > _dutyResults.Count) {
                _currentPage = 0;
            }
            GoToPage();
        }

        private void GoToPage(int? page = null) {
            //null = stay on same page
            page ??= _currentPage;
            _currentPage = (int)page;
            _dutyResultsPage = _dutyResults.OrderByDescending(dr => dr.Time).Skip(_currentPage * _maxPageSize).Take(_maxPageSize).ToList();
            CSV = "";
            foreach(var dutyResult in _dutyResultsPage.OrderBy(dr => dr.Time)) {
                //no checks
                float checkpoint = dutyResult.CheckpointResults.Count / 2f;
                if(_plugin.DutyManager.Duties[dutyResult.DutyId].Structure == DutyStructure.Doors) {
                    checkpoint += 0.5f;
                }
                CSV = CSV + checkpoint.ToString() + ",";
            }
        }

        public void Draw() {
            if(!_refreshLock.Wait(0)) {
                return;
            }
            try {
                _statsWindow.SizeConstraints = new WindowSizeConstraints {
                    MinimumSize = new Vector2(400, _statsWindow.SizeConstraints!.Value.MinimumSize.Y),
                    MaximumSize = _statsWindow.SizeConstraints!.Value.MaximumSize,
                };
                if(_plugin.AllowEdit) {
                    try {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DalamudRed, $"{FontAwesomeIcon.ExclamationTriangle.ToIconString()}");
                    } finally {
                        ImGui.PopFont();
                    }
                    ImGui.SameLine();
                    ImGui.TextColored(ImGuiColors.DalamudRed, Loc.Tr("EDIT MODE ENABLED"));
                    ImGui.SameLine();
                    try {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DalamudRed, $"{FontAwesomeIcon.ExclamationTriangle.ToIconString()}");
                    } finally {
                        ImGui.PopFont();
                    }
                    if(ImGui.Button(Loc.Tr("Save"))) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            _plugin.StorageManager.UpdateDutyResults(_dutyResultsPage.Where(dr => dr.IsEdited));
                            //_plugin.Save();
                        });
                    }

                    ImGui.SameLine();
                    if(ImGui.Button(Loc.Tr("Cancel"))) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            _plugin.AllowEdit = false;
                            _statsWindow.Refresh();
                        });
                    }
                }

                //ImGui.SameLine();
                if(ImGui.Button(Loc.Tr("Copy CSV"))) {
                    Task.Run(() => {
                        ImGui.SetClipboardText(CSV);
                    });
                }
                if(ImGui.IsItemHovered()) {
                    ImGui.BeginTooltip();
                    ImGui.Text(Loc.Tr("Creates a sequential comma-separated list of the last checkpoint reached to the clipboard."));
                    ImGui.EndTooltip();
                }

                ImGui.SameLine();
                if(ImGui.Button(Loc.Tr("Collapse All"))) {
                    _collapseAll = true;
                }

                using(var child = ImRaii.Child("scrolling", new Vector2(0, -(25 + ImGui.GetStyle().ItemSpacing.Y) * ImGuiHelpers.GlobalScale), true)) {
                    if(child) {
                        foreach(var results in _dutyResultsPage) {
                            if(_collapseAll) {
                                ImGui.SetNextItemOpen(false);
                            }

                            float targetWidth1 = 150f * ImGuiHelpers.GlobalScale;
                            float targetWidth2 = 215f * ImGuiHelpers.GlobalScale;
                            var text1 = results.Time.ToString();
                            var text2 = _plugin.DutyManager.Duties[results.DutyId].GetDisplayName();
                            while(ImGui.CalcTextSize(text1).X < targetWidth1) {
                                text1 += " ";
                            }
                            while(ImGui.CalcTextSize(text2).X < targetWidth2) {
                                text2 += " ";
                            }

                            if(ImGui.CollapsingHeader(string.Format("{0:-23} {1:-40} {2:-25}", text1, text2, results.Id.ToString()))) {
                                if(_plugin.AllowEdit) {
                                    DrawDutyResultsEditable(results);
                                } else {
                                    DrawDutyResults(results);
                                }
                            }
                        }
                    }
                }

                ImGui.Text("");
                ImGui.SameLine();

                if(_currentPage > 0) {
                    ImGui.SameLine();
                    if(ImGui.Button(Loc.TrFormat("Previous {0}", _maxPageSize))) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            GoToPage(_currentPage - 1);
                        });
                    }
                }

                if(_dutyResultsPage.Count >= _maxPageSize) {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 65f * ImGuiHelpers.GlobalScale);
                    if(ImGui.Button(Loc.TrFormat("Next {0}", _maxPageSize))) {
                        _plugin.DataQueue.QueueDataOperation(() => {
                            GoToPage(_currentPage + 1);
                        });
                    }
                }
                _collapseAll = false;
            } finally {
                _refreshLock.Release();
            }
        }

        private void DrawDutyResults(DutyResults dutyResults) {
            using(var table = ImRaii.Table($"##{dutyResults.Id}--MainTable", 2, ImGuiTableFlags.NoClip)) {
                if(table) {
                    ImGui.TableNextColumn();
                    using(var subtable1 = ImRaii.Table($"##{dutyResults.Id}--SubTable1", 2, ImGuiTableFlags.NoClip)) {
                        ImGui.TableSetupColumn("propName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 95f);
                        ImGui.TableSetupColumn("propVal", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Completed:"));
                        ImGui.TableNextColumn();
                        ImGui.Text(dutyResults.IsComplete ? Loc.Tr("Yes") : Loc.Tr("No"));

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Duration:"));
                        ImGui.TableNextColumn();
                        string durationString = "";
                        if(dutyResults.CompletionTime > dutyResults.Time) {
                            durationString = (dutyResults.CompletionTime - dutyResults.Time).ToString(@"mm\:ss");
                        }
                        ImGui.Text($"{durationString}");

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Owner:"));
                        ImGui.TableNextColumn();
                        ImGui.Text($"{dutyResults.Owner}");

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Map ID:"));
                        ImGui.TableNextColumn();
                        if(dutyResults.Map != null) {
                            ImGui.Text($"{dutyResults.Map.Id}");
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Last Checkpoint:"));
                        ImGui.TableNextColumn();
                        var currentLastCheckpointIndex = dutyResults.CheckpointResults.Count - 1;
                        if(currentLastCheckpointIndex >= 0) {
                            ImGui.Text($"{_plugin.DutyManager.Duties[dutyResults.DutyId].Checkpoints!.ElementAt(currentLastCheckpointIndex).Name}");
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Total Gil:"));
                        ImGui.TableNextColumn();
                        ImGui.Text($"{dutyResults.TotalGil}");

                        if(_plugin.DutyManager.Duties[dutyResults.DutyId].Structure == DutyStructure.Roulette) {
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Summons:"));
                            ImGui.TableNextColumn();
                            foreach(var checkpointResult in dutyResults.CheckpointResults) {
                                if(checkpointResult.SummonType != null) {
                                    ImGui.Text($"{DutyHelper.GetSummonName((Summon)checkpointResult.SummonType)}");
                                }
                            }
                        }
                    }
                    ImGui.TableNextColumn();
                    using(var subtable2 = ImRaii.Table($"##{dutyResults.Id}--SubTable2", 2)) {
                        ImGui.TableSetupColumn("propName", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 95f);
                        ImGui.TableSetupColumn("propVal", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Party Members:"));
                        ImGui.TableNextColumn();
                        foreach(var partyMember in dutyResults.Players) {
                            ImGui.Text($"{partyMember}");
                        }
                    }
                }
            }
            if(dutyResults.HasLootResults()) {
                ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Loot:"));
                if(_totalGilValue.ContainsKey(dutyResults.Id)) {
                    ImGui.SameLine();
                    string text = Loc.TrFormat("Total Gil Value: {0} (?)", _totalGilValue[dutyResults.Id].ToString("N0"));
                    ImGuiHelper.RightAlignCursor(text);
                    ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Tr("Total Gil Value:"));
                    ImGui.SameLine();
                    ImGui.Text($"{_totalGilValue[dutyResults.Id].ToString("N0")}");
                    ImGui.SameLine();
                    ImGuiHelper.HelpMarker(Loc.Tr("Total market value of all drops plus gil multiplied by number of players."));
                }

                using(ImRaii.Table($"loottable", 4, ImGuiTableFlags.None)) {
                    //ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                    ImGui.TableSetupColumn(Loc.Tr("Quality"), ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 55f);
                    ImGui.TableSetupColumn(Loc.Tr("Name"), ImGuiTableColumnFlags.WidthStretch, ImGuiHelpers.GlobalScale * 200f);
                    ImGui.TableSetupColumn(Loc.Tr("Dropped"), ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 65f);
                    ImGui.TableSetupColumn(Loc.Tr("Obtained"), ImGuiTableColumnFlags.WidthFixed, ImGuiHelpers.GlobalScale * 70f);
                    ImGui.TableNextColumn();
                    ImGui.Text(Loc.Tr("Quality"));
                    ImGui.TableNextColumn();
                    ImGui.Text(Loc.Tr("Name"));
                    ImGui.TableNextColumn();
                    ImGui.Text(Loc.Tr("Dropped"));
                    ImGui.TableNextColumn();
                    ImGui.Text(Loc.Tr("Obtained"));
                    foreach(var lootResult in _lootResults[dutyResults.Id]) {
                        //ImGui.TableNextColumn();
                        //ImGui.Text($"{lootResult.Value.Category}");
                        ImGui.TableNextColumn();
                        var qualityText = lootResult.Key.IsHQ ? "HQ" : "";
                        ImGuiHelper.CenterAlignCursor(qualityText);
                        ImGui.Text($"{qualityText}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{lootResult.Value.ItemName}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{lootResult.Value.DroppedQuantity.ToString("N0")}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{lootResult.Value.ObtainedQuantity.ToString("N0")}");
                    }
                }
            }
        }

        private void DrawDutyResultsEditable(DutyResults dutyResults) {
            List<string> lastCheckpoints = new() {
                "None"
            };
            string? owner = dutyResults.Owner ?? "";
            string? gil = dutyResults.TotalGil.ToString();
            bool isCompleted = dutyResults.IsComplete;
            foreach(var checkpoint in _plugin.DutyManager.Duties[dutyResults.DutyId].Checkpoints!) {
                lastCheckpoints.Add(Loc.Tr(checkpoint.Name));
            }

            if(ImGui.Checkbox(Loc.Tr("Completed") + "##{dutyResults.Id}--Completed", ref isCompleted)) {
                dutyResults.IsEdited = true;
                dutyResults.IsComplete = isCompleted;
            }
            if(ImGui.InputText(Loc.Tr("Owner") + "##{dutyResults.Id}--Owner", ref owner, 50, ImGuiInputTextFlags.AutoSelectAll)) {
                dutyResults.IsEdited = true;
                dutyResults.Owner = owner;
            }
            if(ImGui.InputText(Loc.Tr("Total Gil") + "##{dutyResults.Id}--TotalGil", ref gil, 9, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CharsDecimal)) {
                int gilInt;
                if(int.TryParse(gil, out gilInt)) {
                    dutyResults.IsEdited = true;
                    dutyResults.TotalGil = gilInt;
                }
            }
            var currentLastCheckpointIndex = dutyResults.CheckpointResults.Count;
            if(ImGui.Combo(Loc.Tr("Last Checkpoint") + "##{dutyResults.Id}--LastCheckpoint", ref currentLastCheckpointIndex, lastCheckpoints.ToArray(), lastCheckpoints.Count)) {
                if(currentLastCheckpointIndex > dutyResults.CheckpointResults.Count) {
                    dutyResults.IsEdited = true;
                    for(int i = dutyResults.CheckpointResults.Count; i <= currentLastCheckpointIndex - 1; i++) {
                        dutyResults.CheckpointResults.Add(new CheckpointResults(_plugin.DutyManager.Duties[dutyResults.DutyId].Checkpoints![i], true));
                    }
                } else if(currentLastCheckpointIndex < dutyResults.CheckpointResults.Count) {
                    dutyResults.IsEdited = true;
                    for(int i = dutyResults.CheckpointResults.Count - 1; i >= currentLastCheckpointIndex; i--) {
                        dutyResults.CheckpointResults.RemoveAt(i);
                    }
                }
            }
            if(_plugin.DutyManager.Duties[dutyResults.DutyId].Structure == DutyStructure.Roulette) {
                string[] summons = { Loc.Tr("Lesser"), Loc.Tr("Greater"), Loc.Tr("Elder"), Loc.Tr("Abomination"), Loc.Tr("Circle Shift") };
                var summonCheckpoints = dutyResults.CheckpointResults.Where(cr => cr.Checkpoint.Name.StartsWith("Complete")).ToList();
                for(int i = 0; i < summonCheckpoints.Count(); i++) {
                    int summonIndex = (int?)summonCheckpoints[i].SummonType ?? 3;
                    if(ImGui.Combo(Loc.TrFormat("{0} Summon", StringHelper.AddOrdinal(i + 1)) + "##{summonCheckpoints[i].GetHashCode()}-Summon", ref summonIndex, summons, summons.Length)) {
                        dutyResults.IsEdited = true;
                        summonCheckpoints[i].SummonType = (Summon)summonIndex;
                    }
                }
            }
        }
    }
}





using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using MapPartyAssist.Helper;
using MapPartyAssist.Localization;
using MapPartyAssist.Services;
using MapPartyAssist.Settings;
using MapPartyAssist.Windows;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapPartyAssist {

    public enum StatusLevel {
        OK,
        CAUTION,
        ERROR
    }

    public enum GrammarCase {
        Nominative,
        Accusative,
        Dative,
        Genitive
    }

    public sealed class Plugin : IDalamudPlugin {
        public string Name => "挖宝统计助手";
        private const string DatabaseName = "data.db";
        private static readonly ClientLanguage[] BaseSupportedLanguages = { ClientLanguage.English, ClientLanguage.French, ClientLanguage.German, ClientLanguage.Japanese };

        private static readonly Dictionary<string, uint> BNpcNameFallbackRowIds = new(StringComparer.OrdinalIgnoreCase) {
            { "Altar Airavata", 7601u },
            { "Altar Apanda", 7628u },
            { "Altar Arachne", 7593u },
            { "Altar Beast", 7588u },
            { "Altar Chimera", 7591u },
            { "Altar Diresaur", 7627u },
            { "Altar Dullahan", 7585u },
            { "Altar Kelpie", 7589u },
            { "Altar Mandragora", 7600u },
            { "Altar Manticore", 7629u },
            { "Altar Skatene", 7587u },
            { "Altar Totem", 7586u },
            { "Daen Ose the Avaricious", 9808u },
            { "Fuath Troublemaker", 9786u },
            { "Greedy Pixie", 9797u },
            { "Gymnasiou Acheloios", 12019u },
            { "Gymnasiou Leon", 11997u },
            { "Gymnasiou Mandragoras", 12022u },
            { "Gymnasiou Megakantha", 12009u },
            { "Gymnasiou Meganereis", 12014u },
            { "Gymnasiou Pithekos", 12001u },
            { "Gymnasiou Satyros", 12003u },
            { "Gymnasiou Sphinx", 12016u },
            { "Gymnasiou Styphnolobion", 12012u },
            { "Gymnasiou Tigris", 11999u },
            { "Gymnasiou Triton", 12006u },
            { "Hati", 7590u },
            { "Hippomenes", 12030u },
            { "Lampas Chrysine", 12021u },
            { "Lyssa Chrysine", 12024u },
            { "Narkissos", 12029u },
            { "Phaethon", 12026u },
            { "Secret Basket", 9784u },
            { "Secret Cladoselache", 9778u },
            { "Secret Djinn", 9788u },
            { "Secret Keeper", 9807u },
            { "Secret Korrigan", 9806u },
            { "Secret Pegasus", 9793u },
            { "Secret Porxie", 9795u },
            { "Secret Serpent", 9776u },
            { "Secret Swallow", 9782u },
            { "Secret Undine", 9790u },
            { "Secret Worm", 9780u },
            { "The Great Gold Whisker", 7599u },
            { "The Older One", 7597u },
            { "The Winged", 7595u },
        };

        private static readonly Dictionary<string, Dictionary<ClientLanguage, string>> BNpcNameManualTranslations = new(StringComparer.OrdinalIgnoreCase) {
            { "Altar Airavata", new() { { LanguageHelper.ChineseSimplified, "神殿艾拉瓦塔" } } },
            { "Altar Apanda", new() { { LanguageHelper.ChineseSimplified, "神殿阿班达" } } },
            { "Altar Arachne", new() { { LanguageHelper.ChineseSimplified, "神殿阿剌克涅" } } },
            { "Altar Beast", new() { { LanguageHelper.ChineseSimplified, "神殿巨兽" } } },
            { "Altar Chimera", new() { { LanguageHelper.ChineseSimplified, "神殿奇美拉" } } },
            { "Altar Diresaur", new() { { LanguageHelper.ChineseSimplified, "神殿变种龙" } } },
            { "Altar Dullahan", new() { { LanguageHelper.ChineseSimplified, "神殿无头骑士" } } },
            { "Altar Kelpie", new() { { LanguageHelper.ChineseSimplified, "神殿凯尔派" } } },
            { "Altar Mandragora", new() { { LanguageHelper.ChineseSimplified, "神殿蔓德拉" } } },
            { "Altar Manticore", new() { { LanguageHelper.ChineseSimplified, "神殿曼提克" } } },
            { "Altar Skatene", new() { { LanguageHelper.ChineseSimplified, "神殿斯卡尼特" } } },
            { "Altar Totem", new() { { LanguageHelper.ChineseSimplified, "神殿图腾" } } },
            { "Daen Ose the Avaricious", new() { { LanguageHelper.ChineseSimplified, "视财如命 代恩·奥瑟" } } },
            { "Fuath Troublemaker", new() { { LanguageHelper.ChineseSimplified, "捣乱的水妖" } } },
            { "Greedy Pixie", new() { { LanguageHelper.ChineseSimplified, "寻宝的仙子" } } },
            { "Gymnasiou Acheloios", new() { { LanguageHelper.ChineseSimplified, "育体阿刻罗俄斯" } } },
            { "Gymnasiou Leon", new() { { LanguageHelper.ChineseSimplified, "育体雄狮" } } },
            { "Gymnasiou Mandragoras", new() { { LanguageHelper.ChineseSimplified, "育体蔓德拉" } } },
            { "Gymnasiou Megakantha", new() { { LanguageHelper.ChineseSimplified, "育体巨型刺口花" } } },
            { "Gymnasiou Meganereis", new() { { LanguageHelper.ChineseSimplified, "育体巨型涅瑞伊斯" } } },
            { "Gymnasiou Pithekos", new() { { LanguageHelper.ChineseSimplified, "育体猿猴" } } },
            { "Gymnasiou Satyros", new() { { LanguageHelper.ChineseSimplified, "育体萨提洛斯" } } },
            { "Gymnasiou Sphinx", new() { { LanguageHelper.ChineseSimplified, "育体斯芬克斯" } } },
            { "Gymnasiou Styphnolobion", new() { { LanguageHelper.ChineseSimplified, "育体槐龙" } } },
            { "Gymnasiou Tigris", new() { { LanguageHelper.ChineseSimplified, "育体猛虎" } } },
            { "Gymnasiou Triton", new() { { LanguageHelper.ChineseSimplified, "育体特里同" } } },
            { "Hati", new() { { LanguageHelper.ChineseSimplified, "哈提" } } },
            { "Hippomenes", new() { { LanguageHelper.ChineseSimplified, "希波墨涅斯" } } },
            { "Lampas Chrysine", new() { { LanguageHelper.ChineseSimplified, "金光拉姆帕斯" } } },
            { "Lyssa Chrysine", new() { { LanguageHelper.ChineseSimplified, "金光吕萨" } } },
            { "Narkissos", new() { { LanguageHelper.ChineseSimplified, "纳西索斯" } } },
            { "Phaethon", new() { { LanguageHelper.ChineseSimplified, "法厄同" } } },
            { "Secret Basket", new() { { LanguageHelper.ChineseSimplified, "神秘篮筐" } } },
            { "Secret Cladoselache", new() { { LanguageHelper.ChineseSimplified, "神秘裂口鲨" } } },
            { "Secret Djinn", new() { { LanguageHelper.ChineseSimplified, "神秘镇尼" } } },
            { "Secret Keeper", new() { { LanguageHelper.ChineseSimplified, "神秘守卫" } } },
            { "Secret Korrigan", new() { { LanguageHelper.ChineseSimplified, "神秘柯瑞甘" } } },
            { "Secret Pegasus", new() { { LanguageHelper.ChineseSimplified, "神秘天马" } } },
            { "Secret Porxie", new() { { LanguageHelper.ChineseSimplified, "神秘仙子猪" } } },
            { "Secret Serpent", new() { { LanguageHelper.ChineseSimplified, "神秘巨蟒" } } },
            { "Secret Swallow", new() { { LanguageHelper.ChineseSimplified, "神秘海燕" } } },
            { "Secret Undine", new() { { LanguageHelper.ChineseSimplified, "神秘温蒂尼" } } },
            { "Secret Worm", new() { { LanguageHelper.ChineseSimplified, "神秘巨虫" } } },
            { "The Great Gold Whisker", new() { { LanguageHelper.ChineseSimplified, "金鲶大王" } } },
            { "The Older One", new() { { LanguageHelper.ChineseSimplified, "神殿旧日灵偶" } } },
            { "The Winged", new() { { LanguageHelper.ChineseSimplified, "神殿妖鸟" } } },
        };


        public IEnumerable<ClientLanguage> SupportedLanguages {
            get {
                foreach(var language in BaseSupportedLanguages) {
                    yield return language;
                }

                if(LanguageHelper.TryGetChineseSimplified(out var chineseLanguage)) {
                    yield return chineseLanguage;
                }
            }
        }

        private const string CommandName = "/mparty";
        private const string ConfigCommandName = "/mpartyconfig";
        private const string StatsCommandName = "/mpartystats";
        private const string DutyResultsCommandName = "/mpartydutyresults";
        private const string TestCommandName = "/mpartytest";
        private const string EditCommandName = "/mpartyedit";

        //Dalamud services
        internal IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        internal IDataManager DataManager { get; init; }
        internal IClientState ClientState { get; init; }
        internal ICondition Condition { get; init; }
        internal IDutyState DutyState { get; init; }
        internal IPartyList PartyList { get; init; }
        internal IChatGui ChatGui { get; init; }
        internal IGameGui GameGui { get; init; }
        internal IFramework Framework { get; init; }
        internal IAddonLifecycle AddonLifecycle { get; init; }
        internal IGameInteropProvider InteropProvider { get; init; }
        internal IPluginLog Log { get; init; }

        //Custom services
        internal GameStateManager GameStateManager { get; init; }
        internal DutyManager DutyManager { get; init; }
        internal MapManager MapManager { get; init; }
        internal StorageManager StorageManager { get; init; }
        internal ImportManager ImportManager { get; init; }
        internal MigrationManager MigrationManager { get; init; }
        internal DataQueueService DataQueue { get; init; }
        internal PriceHistoryService PriceHistory { get; init; }

        public Configuration Configuration { get; init; }
        internal GameFunctions Functions { get; init; }

        //UI
        internal WindowSystem WindowSystem = new("Map Party Assist");
        internal MainWindow MainWindow;
        internal StatsWindow StatsWindow;
        internal ConfigWindow ConfigWindow;
#if DEBUG
        internal TestFunctionWindow TestFunctionWindow;
#endif
        //non-persistent configuration options
        internal bool PrintAllMessages { get; set; } = false;
        internal bool PrintPayloads { get; set; } = false;
        internal bool AllowEdit { get; set; } = false;

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IDataManager dataManager,
            IClientState clientState,
            ICondition condition,
            IDutyState dutyState,
            IPartyList partyList,
            IChatGui chatGui,
            IGameGui gameGui,
            IFramework framework,
            IAddonLifecycle addonLifecycle,
            IGameInteropProvider interopProvider,
            IPluginLog log) {
            try {
                PluginInterface = pluginInterface;
                CommandManager = commandManager;
                DataManager = dataManager;
                ClientState = clientState;
                Condition = condition;
                DutyState = dutyState;
                PartyList = partyList;
                ChatGui = chatGui;
                GameGui = gameGui;
                Framework = framework;
                AddonLifecycle = addonLifecycle;
                InteropProvider = interopProvider;
                Log = log;

                AtkNodeHelper.Log = Log;

                Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

                Log.Information($"Client language: {ClientState.ClientLanguage}");
                Log.Verbose($"Current culture: {CultureInfo.CurrentCulture.Name}");
                if(!IsLanguageSupported()) {
                    Log.Warning("Client language unsupported, most functions will be unavailable.");
                }

                //order is important here
                DataQueue = new(this);
                StorageManager = new(this, $"{PluginInterface.GetPluginConfigDirectory()}\\{DatabaseName}");
                Functions = new(this);
                DutyManager = new(this);
                MapManager = new(this);
                ImportManager = new(this);
                MigrationManager = new(this);
                Configuration.Initialize(this);
                GameStateManager = new(this);
                PriceHistory = new(this);

                MainWindow = new MainWindow(this);
                WindowSystem.AddWindow(MainWindow);
                CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
                    HelpMessage = Loc.Tr("Opens map tracker console.")
                });

                StatsWindow = new StatsWindow(this);
                WindowSystem.AddWindow(StatsWindow);
                CommandManager.AddHandler(StatsCommandName, new CommandInfo(OnStatsCommand) {
                    HelpMessage = Loc.Tr("Opens stats window.")
                });

                ConfigWindow = new ConfigWindow(this);
                WindowSystem.AddWindow(ConfigWindow);
                CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnConfigCommand) {
                    HelpMessage = Loc.Tr("Open settings window.")
                });

#if DEBUG
                TestFunctionWindow = new TestFunctionWindow(this);
                WindowSystem.AddWindow(TestFunctionWindow);
                CommandManager.AddHandler(TestCommandName, new CommandInfo(OnTestCommand) {
                    HelpMessage = Loc.Tr("Opens test functions window. (Debug)")
                });
#endif

                CommandManager.AddHandler(EditCommandName, new CommandInfo(OnEditCommand) {
                    HelpMessage = Loc.Tr("Toggle editing of maps/duty results.")
                });

                PluginInterface.UiBuilder.Draw += DrawUI;
                PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
                PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

                ChatGui.CheckMessageHandled += OnChatMessage;

                //data migration
                DataQueue.QueueDataOperation(Initialize);

                Log.Information("Map Party Assist has started.");
            } catch(Exception e) {
                //remove handlers and release database if we fail to start
                Dispose();
                //it really shouldn't ever be null
                Log!.Error($"Failed to initialize plugin constructor: {e.Message}");
                //re-throw to prevent constructor from initializing
                throw;
            }
        }

        private async Task Initialize() {
            var lastVersion = new Version(Configuration.LastPluginVersion);
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            //MigrationManager.CheckAndMigrate();

            //version update validations
            var validationTask = Task.Run(() => {
                if(lastVersion < new Version(2, 5, 0, 1)) {
                    MigrationManager.SetClearedDutiesToComplete();
                }
            });
            await Task.WhenAll(validationTask);

            Configuration.LastPluginVersion = currentVersion?.ToString() ?? "0.0.0.0";
            Configuration.Save();
            Refresh();
            Log.Information("Map Party Assist initialized.");
        }

        //Custom config loader. Unused
        public IPluginConfiguration? GetPluginConfig() {
            //string pluginName = PluginInterface.InternalName;
            FileInfo configFile = PluginInterface.ConfigFile;
            if(!configFile.Exists) {
                return null;
            }
            return JsonConvert.DeserializeObject<IPluginConfiguration>(File.ReadAllText(configFile.FullName), new JsonSerializerSettings {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        public void Dispose() {
#if DEBUG
            Log.Debug("插件正在卸载");
#endif

            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(ConfigCommandName);
            CommandManager.RemoveHandler(StatsCommandName);
            CommandManager.RemoveHandler(EditCommandName);

#if DEBUG
            CommandManager.RemoveHandler(TestCommandName);
#endif

            ChatGui.CheckMessageHandled -= OnChatMessage;

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

            MapManager?.Dispose();
            DutyManager?.Dispose();
            StorageManager?.Dispose();
            GameStateManager?.Dispose();
            DataQueue?.Dispose();
            PriceHistory?.Dispose();
        }

        private void OnCommand(string command, string args) {
            MainWindow.IsOpen = true;
        }

        private void OnStatsCommand(string command, string args) {
            StatsWindow.IsOpen = true;
        }

        private void OnConfigCommand(string command, string args) {
            DrawConfigUI();
        }

#if DEBUG
        private void OnTestCommand(string command, string args) {
            TestFunctionWindow.IsOpen = true;
        }
#endif

        private void OnEditCommand(string command, string args) {
            AllowEdit = !AllowEdit;
            ChatGui.Print($"挖宝统计助手编辑模式：{(AllowEdit ? "开启" : "关闭")}");
        }

        private void DrawUI() {
            WindowSystem.Draw();
        }

        private void DrawConfigUI() {
            ConfigWindow.IsOpen = true;
        }

        private void DrawMainUI() {
            StatsWindow.IsOpen = true;
        }

        private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled) {
            //filter nuisance combat messages...
            switch((int)type) {
                case 2091:  //self actions
                case 4139:  //party member actions
                    if(Regex.IsMatch(message.ToString(), @"(Dig|Excavation|Ausgraben|ディグ|挖掘)", RegexOptions.IgnoreCase)) {
                        goto case 2105;
                    }
                    goto default;
                case 2233:
                case 2105:  //system messages of some kind
                case 2361:
                case 62:    //self gil
                case 2110:  //self loot obtained
                case 4158:  //party loot obtained
                case 8254:  //alliance loot obtained
                case (int)XivChatType.Say:
                case (int)XivChatType.Party:
                case (int)XivChatType.SystemMessage:
                    //Log.Verbose($"Message received: {type} {message} from {sender}");
                    Log.Debug(String.Format("type: {0,-6} sender: {1,-20} message: {2}", type, sender, message));
                    if(PrintPayloads) {
                        foreach(Payload payload in message.Payloads) {
                            Log.Debug($"payload: {payload}");
                        }
                    }
                    break;
                default:
                    if(PrintAllMessages) {
                        goto case 2105;
                    }
                    break;
            }
        }

        public void OpenMapLink(MapLinkPayload mapLink) {
            GameGui.OpenMapWithMapLink(mapLink);
        }

        public void Refresh() {
            Configuration.Save();
            StatsWindow?.Refresh();
            MainWindow?.Refresh();
        }

        public bool IsLanguageSupported(ClientLanguage? language = null) {
            language ??= ClientState.ClientLanguage;
            return SupportedLanguages.Contains((ClientLanguage)language);
        }

        public string TranslateBNpcName(string npcName, ClientLanguage destinationLanguage, ClientLanguage? originLanguage = null) {
            return TranslateDataTableEntry<BNpcName>(npcName, "Singular", GrammarCase.Nominative, destinationLanguage, originLanguage);
        }

        public uint? ResolveItemRowId(string itemName, bool usePluralForm, ClientLanguage? languageOverride = null) {
            var languageToUse = languageOverride ?? ClientState.ClientLanguage;
            var casesToTry = new[] {
                GrammarCase.Accusative,
                GrammarCase.Nominative,
                GrammarCase.Dative,
                GrammarCase.Genitive
            };

            foreach(var gramCase in casesToTry) {
                try {
                    var rowIdAttempt = usePluralForm
                        ? GetRowId<Item>(itemName, "Plural", gramCase, languageToUse)
                        : GetRowId<Item>(itemName, "Singular", gramCase, languageToUse);
                    if(rowIdAttempt.HasValue) {
                        return rowIdAttempt;
                    }
                } catch(ArgumentException) {
                    //language not supported for this case/column combination, continue to next attempt
                }
            }

            try {
                var sheet = DataManager.GetExcelSheet<Item>(languageToUse);
                if(sheet != null) {
                    string normalizedTarget = NormalizeComparisonString(itemName);
                    foreach(var row in sheet) {
                        string normalizedSingular = NormalizeComparisonString(row.Singular.ToString());
                        if(!string.IsNullOrEmpty(normalizedSingular) && string.Equals(normalizedTarget, normalizedSingular, StringComparison.OrdinalIgnoreCase)) {
                            return row.RowId;
                        }

                        if(usePluralForm) {
                            string normalizedPlural = NormalizeComparisonString(row.Plural.ToString());
                            if(!string.IsNullOrEmpty(normalizedPlural) && string.Equals(normalizedTarget, normalizedPlural, StringComparison.OrdinalIgnoreCase)) {
                                return row.RowId;
                            }
                        }
                    }
                }
            } catch(Exception ex) {
                Log.Warning(ex, $"Unable to resolve item row id for '{itemName}' in language {languageToUse}.");
            }

            if(LanguageHelper.TryGetChineseSimplified(out var chineseLanguage) && languageToUse != chineseLanguage) {
                var chineseFallback = ResolveItemRowId(itemName, usePluralForm, chineseLanguage);
                if(chineseFallback.HasValue) {
                    return chineseFallback;
                }
            }

            return null;
        }

        public string TranslateDataTableEntry<T>(string data, string column, GrammarCase gramCase, ClientLanguage destinationLanguage, ClientLanguage? originLanguage = null) where T : struct, IExcelRow<T> {
            originLanguage ??= ClientState.ClientLanguage;
            uint? rowId = null;
            Type type = typeof(T);
            bool isPlural = column.Equals("Plural", StringComparison.OrdinalIgnoreCase);
            string normalizedTarget = NormalizeComparisonString(data);

            if(!IsLanguageSupported(destinationLanguage) || !IsLanguageSupported(originLanguage)) {
                throw new ArgumentException("无法在未受支持的客户端语言之间进行翻译。");
            }

            //check to make sure column is string
            var columnProperty = type.GetProperty(column) ?? throw new ArgumentException($"类型 {type.FullName} 上不存在名称为 {column} 的属性。");
            if(!columnProperty.PropertyType.IsAssignableTo(typeof(ReadOnlySeString))) {
                throw new ArgumentException($"类型 {type.FullName} 的属性 {column}（{columnProperty.PropertyType.FullName}）无法转换为 SeString。");
            }

            //iterate over table to find rowId
            foreach(var row in DataManager.GetExcelSheet<T>((ClientLanguage)originLanguage)!) {
                var rowData = columnProperty!.GetValue(row)?.ToString();

                if(string.IsNullOrEmpty(rowData)) {
                    continue;
                }

                //German declension placeholder replacement
                if(originLanguage == ClientLanguage.German) {
                    var pronounProperty = type.GetProperty("Pronoun");
                    if(pronounProperty != null) {
                        int pronoun = Convert.ToInt32(pronounProperty.GetValue(row))!;
                        rowData = ReplaceGermanDeclensionPlaceholders(rowData, pronoun, isPlural, gramCase);
                    }
                }

                string normalizedRowData = NormalizeComparisonString(rowData);
                if(string.Equals(normalizedTarget, normalizedRowData, StringComparison.OrdinalIgnoreCase)) {
                    rowId = row.RowId;
                    break;
                }
            }

            if(!rowId.HasValue && typeof(T) == typeof(BNpcName) && originLanguage == ClientLanguage.English) {
                if(BNpcNameFallbackRowIds.TryGetValue(data, out var fallbackRowId)) {
                    rowId = fallbackRowId;
                }
            }

            if(!rowId.HasValue) {
                if(typeof(T) == typeof(BNpcName) && TryGetBNpcNameManualTranslation(data, destinationLanguage, out var manualTranslation)) {
                    return manualTranslation;
                }

                Log.Warning($"'{data}' not found in table: {type.Name} for language: {originLanguage}. Using original value.");
                return data;
            }

            //get data from destinationLanguage
            bool translatedRowFound = false;
            T translatedRow = default;
            foreach(var row in DataManager.GetExcelSheet<T>(destinationLanguage)!) {
                if(row.RowId == rowId) {
                    translatedRow = row;
                    translatedRowFound = true;
                    break;
                }
            }

            if(!translatedRowFound) {
                if(typeof(T) == typeof(BNpcName) && TryGetBNpcNameManualTranslation(data, destinationLanguage, out var manualTranslation)) {
                    return manualTranslation;
                }

                Log.Warning($"Row id {rowId} not found in table {type.Name} for language: {destinationLanguage}. Using original value.");
                return data;
            }

            string? translatedString = columnProperty!.GetValue(translatedRow)?.ToString();
            if(string.IsNullOrEmpty(translatedString)) {
                if(typeof(T) == typeof(BNpcName) && TryGetBNpcNameManualTranslation(data, destinationLanguage, out var manualTranslation)) {
                    return manualTranslation;
                }

                Log.Warning($"Translation for row {rowId} in table {type.Name} and column {column} for language {destinationLanguage} is empty. Using original value.");
                return data;
            }

            //add German declensions.
            if(destinationLanguage == ClientLanguage.German) {
                var pronounProperty = type.GetProperty("Pronoun");
                if(pronounProperty != null) {
                    int pronoun = Convert.ToInt32(pronounProperty.GetValue(translatedRow))!;
                    translatedString = ReplaceGermanDeclensionPlaceholders(translatedString, pronoun, isPlural, gramCase);
                }
            }

            return translatedString;
        }
        private static bool TryGetBNpcNameManualTranslation(string data, ClientLanguage destinationLanguage, out string translation) {
            translation = string.Empty;

            if(!BNpcNameManualTranslations.TryGetValue(data, out var translations)) {
                return false;
            }

            if(translations.TryGetValue(destinationLanguage, out var value)) {
                translation = value;
                return true;
            }

            if(LanguageHelper.TryGetChineseSimplified(out var chineseLanguage) && destinationLanguage == chineseLanguage && translations.TryGetValue(chineseLanguage, out value)) {
                translation = value;
                return true;
            }

            return false;
        }

        private static string NormalizeComparisonString(string? value) {
            if(string.IsNullOrWhiteSpace(value)) {
                return string.Empty;
            }

            var normalized = Regex.Replace(value.Trim(), "[\\uE000-\\uF8FF]", string.Empty);
            return Regex.Replace(normalized, "\\s+", " ");
        }
        //male = 0, female = 1, neuter = 2
        private static string ReplaceGermanDeclensionPlaceholders(string input, int gender, bool isPlural, GrammarCase gramCase) {
            if(isPlural) {
                switch(gramCase) {
                    case GrammarCase.Nominative:
                    case GrammarCase.Accusative:
                    default:
                        input = input.Replace("[a]", "e").Replace("[t]", "die");
                        break;
                    case GrammarCase.Dative:
                        input = input.Replace("[a]", "en").Replace("[t]", "den");
                        break;
                    case GrammarCase.Genitive:
                        input = input.Replace("[a]", "er").Replace("[t]", "der");
                        break;
                }
            }
            switch(gender) {
                default:
                case 0: //male
                    switch(gramCase) {
                        case GrammarCase.Nominative:
                        default:
                            input = input.Replace("[a]", "er").Replace("[t]", "der");
                            break;
                        case GrammarCase.Accusative:
                            input = input.Replace("[a]", "en").Replace("[t]", "den");
                            break;
                        case GrammarCase.Dative:
                            input = input.Replace("[a]", "em").Replace("[t]", "dem");
                            break;
                        case GrammarCase.Genitive:
                            input = input.Replace("[a]", "es").Replace("[t]", "des");
                            break;
                    }
                    break;
                case 1: //female
                    switch(gramCase) {
                        case GrammarCase.Nominative:
                        case GrammarCase.Accusative:
                        default:
                            input = input.Replace("[a]", "e").Replace("[t]", "die");
                            break;
                        case GrammarCase.Dative:
                        case GrammarCase.Genitive:
                            input = input.Replace("[a]", "er").Replace("[t]", "der");
                            break;
                    }
                    break;
                case 2: //neuter
                    switch(gramCase) {
                        case GrammarCase.Nominative:
                        case GrammarCase.Accusative:
                        default:
                            input = input.Replace("[a]", "es").Replace("[t]", "das");
                            break;
                        case GrammarCase.Dative:
                            input = input.Replace("[a]", "em").Replace("[t]", "dem");
                            break;
                        case GrammarCase.Genitive:
                            input = input.Replace("[a]", "es").Replace("[t]", "des");
                            break;
                    }
                    break;
            }
            //remove possessive placeholder
            input = input.Replace("[p]", "");
            return input;
        }

        public uint? GetRowId<T>(string data, string column, GrammarCase gramCase, ClientLanguage? language = null) where T : struct, IExcelRow<T> {
            language ??= ClientState.ClientLanguage;
            Type type = typeof(T);
            bool isPlural = column.Equals("Plural", StringComparison.OrdinalIgnoreCase);

            if(!IsLanguageSupported(language)) {
                throw new ArgumentException($"不支持的语言：{language}");
            }

            //check to make sure column is string
            var columnProperty = type.GetProperty(column) ?? throw new ArgumentException($"类型 {type.FullName} 上不存在名称为 {column} 的属性。");
            if(!columnProperty.PropertyType.IsAssignableTo(typeof(ReadOnlySeString))) {
                throw new ArgumentException($"类型 {type.FullName} 的属性 {column}（{columnProperty.PropertyType.FullName}）无法转换为 SeString。");
            }

            //iterate over table to find rowId
            foreach(var row in DataManager.GetExcelSheet<T>((ClientLanguage)language)!) {
                var rowData = columnProperty!.GetValue(row)?.ToString();

                //German declension placeholder replacement
                if(language == ClientLanguage.German && rowData != null) {
                    var pronounProperty = type.GetProperty("Pronoun");
                    if(pronounProperty != null) {
                        int pronoun = Convert.ToInt32(pronounProperty.GetValue(row))!;
                        rowData = ReplaceGermanDeclensionPlaceholders(rowData, pronoun, isPlural, gramCase);
                    }
                }
                if(data.Equals(rowData, StringComparison.OrdinalIgnoreCase)) {
                    return row.RowId;
                }
            }
            return null;
        }
    }
}













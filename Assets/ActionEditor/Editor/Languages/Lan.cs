using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ActionEditor
{
    internal static class Lan
    {
        internal static readonly Dictionary<string, ILanguages> AllLanguages =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string,
            IReadOnlyDictionary<string, string>> LanguageValues =
            new(StringComparer.Ordinal);

        private static string currentLanguage;
        internal static ILanguages ins;

        internal static string Language => currentLanguage;

        internal static void Load()
        {
            if (!AllLanguages.ContainsKey("简体中文"))
                Register("简体中文", new LanCHS());
            if (!AllLanguages.ContainsKey("English"))
                Register("English", new LanEN());
            RegisterFrameworkTexts();

            string preferred = Prefs.Lan_key;
            if (string.IsNullOrEmpty(preferred) ||
                !AllLanguages.ContainsKey(preferred))
                preferred = AllLanguages.Keys.First();
            SetLanguage(preferred);
        }

        internal static void Register(string displayName, ILanguages language)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Language name cannot be empty.",
                    nameof(displayName));
            AllLanguages[displayName] = language ??
                throw new ArgumentNullException(nameof(language));
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            PropertyInfo[] properties = typeof(ILanguages).GetProperties();
            for (int i = 0; i < properties.Length; i++)
                values[properties[i].Name] =
                    (string)properties[i].GetValue(language);
            LanguageValues[displayName] = values;
        }

        internal static void Register(string displayName,
            IReadOnlyDictionary<string, string> values)
        {
            Register(displayName, new DictionaryLanguage(values));
            LanguageValues[displayName] = values;
        }

        internal static bool Remove(string displayName)
        {
            if (string.Equals(displayName, currentLanguage,
                StringComparison.Ordinal)) return false;
            LanguageValues.Remove(displayName);
            return AllLanguages.Remove(displayName);
        }

        internal static void SetLanguage(string key)
        {
            if (!AllLanguages.TryGetValue(key, out ILanguages language)) return;
            currentLanguage = key;
            ins = language;
            Prefs.Lan_key = key;
            Prefs.Save();
        }

        internal static string Text(string key, string fallback = null)
        {
            if (ins == null) Load();
            if (LanguageValues.TryGetValue(currentLanguage, out var values) &&
                values.TryGetValue(key, out string value) &&
                !string.IsNullOrEmpty(value)) return value;
            if (LanguageValues.TryGetValue("English", out values) &&
                values.TryGetValue(key, out value) &&
                !string.IsNullOrEmpty(value)) return value;
            return fallback ?? key;
        }

        private static void RegisterFrameworkTexts()
        {
            AddText("UnsavedChanges", "未保存的更改", "Unsaved Changes");
            AddText("SaveChangesPrompt", "打开其他文件前是否保存对“{0}”的更改？",
                "Save changes to \"{0}\" before opening another file?");
            AddText("Cancel", "取消", "Cancel");
            AddText("DontSave", "不保存", "Don't Save");
            AddText("Create", "新建", "Create");
            AddText("ShowInProject", "在 Project 中显示", "Show in Project");
            AddText("Inspector", "检视面板", "Inspector");
            AddText("UndoHistory", "撤销历史", "Undo History");
            AddText("NoUndoHistory", "没有撤销历史", "No Undo History");
            AddText("Settings", "设置", "Settings");
            AddText("None", "无", "None");
            AddText("Nodes", "节点", "Nodes");
            AddText("Tree", "树", "Tree");
            AddText("Comment", "注释", "Comment");
            AddText("CreateNode", "创建节点", "Create Node");
            AddText("CreateComment", "创建注释", "Create Comment");
            AddText("CreateGroup", "创建分组", "Create Group");
            AddText("DuplicateSelection", "复制所选", "Duplicate Selection");
            AddText("FrameSelection", "聚焦所选", "Frame Selection");
            AddText("SelectAll", "全选", "Select All");
            AddText("DeleteSelection", "删除所选", "Delete Selection");
            AddText("DisconnectAll", "断开全部连线", "Disconnect All");
            AddText("RemoveFromGroup", "移出分组", "Remove From Group");
            AddText("PrettyLayoutOutputs", "整理输出布局", "Layout Outputs");
            AddText("DeleteSelf", "仅删除分组", "Delete Group Only");
            AddText("RemoveNodes", "移出全部节点", "Remove Nodes");
            AddText("RemoveSelectedNodes", "移出所选节点",
                "Remove Selected Nodes");
            AddText("AddSelectedNodes", "加入所选节点", "Add Selected Nodes");
            AddText("Select", "选择", "Select");
            AddText("SyncSubTree", "同步中断、信号量和事件",
                "Sync Interrupts, Semaphores and Events");
            AddText("Blackboard", "黑板", "Blackboard");
            AddText("Events", "事件", "Events");
            AddText("InterruptFlags", "中断标记", "Interrupt Flags");
            AddText("Semaphores", "信号量", "Semaphores");
            AddText("RootNodeNotFound", "未找到根节点", "Root Node Not Found");
            AddText("TimelineSettings", "时间轴", "Timeline");
            AddText("NodeGraphSettings", "节点图", "Node Graph");
            AddText("Blend", "混合", "Blend");
            AddText("Title", "标题", "Title");
            AddText("FontSize", "字号", "Font Size");
            AddText("MiniMap", "小地图", "Mini Map");
            AddText("InPoint", "入点", "IN");
            AddText("OutPoint", "出点", "OUT");
            AddText("ActionEditor", "动作时间轴编辑器", "Action Editor");
            AddText("ActionNodeEditor", "动作节点编辑器",
                "Action Node Editor");
        }

        private static void AddText(string key, string chinese, string english)
        {
            if (LanguageValues.TryGetValue("简体中文", out var chineseValues) &&
                chineseValues is Dictionary<string, string> chineseTable)
                chineseTable[key] = chinese;
            if (LanguageValues.TryGetValue("English", out var englishValues) &&
                englishValues is Dictionary<string, string> englishTable)
                englishTable[key] = english;
        }

        private sealed class DictionaryLanguage : ILanguages
        {
            private readonly IReadOnlyDictionary<string, string> values;

            internal DictionaryLanguage(IReadOnlyDictionary<string, string> values)
            {
                this.values = values ??
                    throw new ArgumentNullException(nameof(values));
            }

            internal string Get(string key, string fallback = null) =>
                values.TryGetValue(key, out string value) &&
                !string.IsNullOrEmpty(value) ? value : fallback ?? key;

            public string AssetPickListType => Get(nameof(AssetPickListType));
            public string Language => Get(nameof(Language));
            public string Title => Get(nameof(Title));
            public string CreateAsset => Get(nameof(CreateAsset));
            public string CrateAssetType => Get(nameof(CrateAssetType));
            public string CrateAssetName => Get(nameof(CrateAssetName));
            public string CreateAssetFileName => Get(nameof(CreateAssetFileName));
            public string CreateAssetConfirm => Get(nameof(CreateAssetConfirm));
            public string CreateAssetConfirmBySelectPath =>
                Get(nameof(CreateAssetConfirmBySelectPath));
            public string CreateAssetTipsNameNull =>
                Get(nameof(CreateAssetTipsNameNull));
            public string CreateAssetTipsRepetitive =>
                Get(nameof(CreateAssetTipsRepetitive));
            public string Preferences => Get(nameof(Preferences));
            public string StepMode => Get(nameof(StepMode));
            public string SnapInterval => Get(nameof(SnapInterval));
            public string FrameRate => Get(nameof(FrameRate));
            public string MagnetSnapping => Get(nameof(MagnetSnapping));
            public string SavePath => Get(nameof(SavePath));
            public string AutoSaveTime => Get(nameof(AutoSaveTime));
            public string Select => Get(nameof(Select));
            public string SelectFolder => Get(nameof(SelectFolder));
            public string TipsTitle => Get(nameof(TipsTitle));
            public string TipsConfirm => Get(nameof(TipsConfirm));
            public string Disable => Get(nameof(Disable));
            public string Locked => Get(nameof(Locked));
            public string Save => Get(nameof(Save));
            public string SaveAs => Get(nameof(SaveAs));
            public string HeaderLastSaveTime => Get(nameof(HeaderLastSaveTime));
            public string MenuAddTrack => Get(nameof(MenuAddTrack));
            public string GroupAdd => Get(nameof(GroupAdd));
            public string ClearCopy => Get(nameof(ClearCopy));
            public string Copy => Get(nameof(Copy));
            public string Cut => Get(nameof(Cut));
            public string Delete => Get(nameof(Delete));
            public string MatchClipLength => Get(nameof(MatchClipLength));
            public string Paste => Get(nameof(Paste));
            public string NotSelectAsset => Get(nameof(NotSelectAsset));
            public string OverflowInvalid => Get(nameof(OverflowInvalid));
            public string EndTimeOverflowInvalid =>
                Get(nameof(EndTimeOverflowInvalid));
            public string StartTimeOverflowInvalid =>
                Get(nameof(StartTimeOverflowInvalid));
            public string ClipInvalid => Get(nameof(ClipInvalid));
            public string ClearSelect => Get(nameof(ClearSelect));
            public string NoAssetExtendType => Get(nameof(NoAssetExtendType));
        }
    }
}

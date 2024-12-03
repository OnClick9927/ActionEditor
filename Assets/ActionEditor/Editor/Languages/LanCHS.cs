namespace ActionEditor
{
    [Name("简体中文")]
    public class LanCHS : ILanguages
    {

        //**********  Welcome *********
        public string Title => "行为时间轴编辑器";
        public string CreateAsset => "创建时间轴";
        public string SelectAsset => "选择时间轴";
        public string Seeting => "编辑器配置";


        //**********  Crate Window *********
        public string CrateAssetType => "创建类型";
        public string CrateAssetName => "时间轴名称";
        public string CreateAssetFileName => "时间轴的文件名称";
        public string CreateAssetConfirm => "创建";
        public string CreateAssetReset => "重置";
        public string CreateAssetTipsNameNull => "名称不能为空";
        public string CreateAssetTipsRepetitive => "已存在同名时间轴";


        //**********  Preferences Window *********
        public string PreferencesTitle => "编辑器首选项";
        public string PreferencesTimeStepMode => "时间步长模式";
        public string PreferencesSnapInterval => "时间步长";
        public string PreferencesFrameRate => "帧率";
        public string PreferencesMagnetSnapping => "剪辑吸附";
        public string PreferencesMagnetSnappingTips => "是否开启剪辑自动吸附前后其他剪辑";
        public string PreferencesScrollWheelZooms => "滚轮缩放";
        public string PreferencesScrollWheelZoomsTips => "是否开启滚轮缩放时间轴区域";
        public string PreferencesSavePath => "配置保存地址";
        public string PreferencesSavePathTips => "创建和选择时的默认地址";
        public string PreferencesAutoSaveTime => "自动保存时间";
        public string PreferencesAutoSaveTimeTips => "定时自动保存操作的间隔时间";
        //public string PreferencesHelpDoc => "帮助文档";


        //**********  Commom *********
        public string Select => "选择";
        public string SelectFile => "选择文件";
        public string SelectFolder => "选择文件夹";
        public string TipsTitle => "提示";
        public string TipsConfirm => "确定";
        public string TipsCancel => "取消";
        public string CompilingTips => "编译中\n...请稍后...";
        public string Disable => "禁用";
        public string Locked => "锁定";
        public string Save => "保存";
        public string Rename => "重命名";

        //**********  Header *********
        public string HeaderLastSaveTime => "最后保存时间：{0}";
        public string HeaderSelectAsset => "选中：[{0}]";
        public string OpenPreferencesTips => "打开首选项界面";
        public string SelectAssetTips => "点击切换时间轴";
        public string OpenMagnetSnappingTips => "开启剪辑磁性吸附";
        public string NewAssetTips => "新建时间轴";
        public string BackMenuTips => "返回主菜单";
        public string PlayLoopTips => "循环播放";
        public string PlayForwardTips => "跳转结尾处";
        public string StepForwardTips => "跳转下一帧";
        public string PauseTips => "点击暂停";
        public string PlayTips => "点击播放";
        public string StopTips => "点击停止播放";
        public string StepBackwardTips => "跳转上一帧";

        //**********  Group Menu *********
        public string MenuAddTrack => "添加轨道";
        public string MenuPasteTrack => "粘贴轨道";
        public string GroupAdd => "添加组";
        public string GroupDisable => "禁用组";
        public string GroupLocked => "锁定组";
        public string GroupReplica => "复制组";
        public string GroupDelete => "删除组";
        public string GroupDeleteTips => "确定删除组吗?";
        public string FirstFrame => "回到第一帧";


        //**********  Track Menu *********
        public string TrackDisable => "禁用轨道";
        public string TrackLocked => "锁定轨道";
        public string TrackCopy => "拷贝轨道";
        public string TrackCut => "剪切轨道";
        public string TrackDelete => "删除轨道";
        public string TrackDeleteTips => "确定删除改轨道吗?";

        //**********  Clip Menu *********
        public string ClipCopy => "拷贝";
        public string ClipCut => "剪切";
        public string ClipDelete => "删除";
        public string MatchClipLength => "匹配长度";
        public string MatchPreviousLoop => "匹配上个循环";
        public string MatchNextLoop => "匹配下个循环";
        public string ClipPaste => "粘贴 ({0})";

        //**********  Inspector *********
        public string NotSelectAsset => "当前没有选中的时间轴对象。";
        public string InsBaseInfo => "{0} 基础信息";
        public string OverflowInvalid => "剪辑超出有效范围";
        public string EndTimeOverflowInvalid => "剪辑结束时间超出有效范围";
        public string StartTimeOverflowInvalid => "剪辑开始时间超出可播放范围";

        public string ClipInvalid => "该片段无效，检查参数";

        public string ClearCopy => "取消拷贝或者复制";

        public string EmptyRect => "空白区域\n点击清空选择";
    }
}
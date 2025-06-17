namespace ActionEditor
{
    [Name("简体中文")]
     class LanCHS : ILanguages
    {
        string ILanguages.Clip => "片段";

        string ILanguages.Track => "轨道";

        //**********  Welcome *********
        string ILanguages.Title => "行为时间轴编辑器";
        string ILanguages.CreateAsset => "新建";


        //**********  Crate Window *********
        string ILanguages.CrateAssetType => "创建类型";
        string ILanguages.CrateAssetName => "时间轴名称";
        string ILanguages.CreateAssetFileName => "时间轴的文件名称";
        string ILanguages.CreateAssetConfirm => "默认路径创建";
        string ILanguages.CreateAssetConfirmBySelectPath => "选择路径创建";

        string ILanguages.CreateAssetTipsNameNull => "名称不能为空";
        string ILanguages.CreateAssetTipsRepetitive => "已存在同名时间轴";


        //**********  Preferences Window *********
        string ILanguages.PreferencesTitle => "编辑器首选项";
        string ILanguages.PreferencesTimeStepMode => "时间步长模式";
        string ILanguages.PreferencesSnapInterval => "时间步长";
        string ILanguages.PreferencesFrameRate => "帧率";
        string ILanguages.PreferencesMagnetSnapping => "剪辑吸附";
        string ILanguages.PreferencesMagnetSnappingTips => "是否开启剪辑自动吸附前后其他剪辑";
        string ILanguages.PreferencesSavePath => "配置保存地址";
        string ILanguages.PreferencesSavePathTips => "创建和选择时的默认地址";
        string ILanguages.PreferencesAutoSaveTime => "自动保存时间";
        string ILanguages.PreferencesAutoSaveTimeTips => "定时自动保存操作的间隔时间";


        //**********  Commom *********
        string ILanguages.Select => "选择";
        string ILanguages.SelectFolder => "选择文件夹";
        string ILanguages.TipsTitle => "提示";
        string ILanguages.TipsConfirm => "确定";
        string ILanguages.Disable => "禁用";
        string ILanguages.Locked => "锁定";
        string ILanguages.Save => "保存";

        //**********  Header *********
        string ILanguages.HeaderLastSaveTime => "最后保存时间：{0}";


        //**********  Group Menu *********
        string ILanguages.MenuAddTrack => "添加轨道";
        string ILanguages.GroupAdd => "添加组";






        //**********  Clip Menu *********
        string ILanguages.Copy => "拷贝";
        string ILanguages.Cut => "剪切";
        string ILanguages.Delete => "删除";
        string ILanguages.MatchClipLength => "匹配长度";
        string ILanguages.Paste => "粘贴 ({0})";

        //**********  Inspector *********
        string ILanguages.NotSelectAsset => "当前没有选中的时间轴对象。";
        string ILanguages.OverflowInvalid => "剪辑超出有效范围";
        string ILanguages.EndTimeOverflowInvalid => "剪辑结束时间超出有效范围";
        string ILanguages.StartTimeOverflowInvalid => "剪辑开始时间超出可播放范围";

        string ILanguages.ClipInvalid => "该片段无效，检查参数";

        string ILanguages.ClearCopy => "清空拷贝/复制";

        string ILanguages.ClearSelect => "清空选择";

        string ILanguages.NoAssetExtendType => "没有 继承于 Asset 的 类型";

    }
}
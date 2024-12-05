namespace ActionEditor
{
    interface ILanguages
    {
        //**********  Welcome *********
        string Title { get; }
        string CreateAsset { get; }
        string SelectAsset { get; }
        string Seeting { get; }


        //**********  Crate Window *********
        string CrateAssetType { get; }
        string CrateAssetName { get; }
        string CreateAssetFileName { get; }
        string CreateAssetConfirm { get; }
        // string CreateAssetReset { get; }
        string CreateAssetTipsNameNull { get; }
        string CreateAssetTipsRepetitive { get; }


        //**********  Preferences Window *********
        string PreferencesTitle { get; }
        string PreferencesTimeStepMode { get; }
        string PreferencesSnapInterval { get; }
        string PreferencesFrameRate { get; }
        string PreferencesMagnetSnapping { get; }
        string PreferencesMagnetSnappingTips { get; }
        // string PreferencesScrollWheelZooms { get; }
        string PreferencesScrollWheelZoomsTips { get; }
        string PreferencesSavePath { get; }
        string PreferencesSavePathTips { get; }
        string PreferencesAutoSaveTime { get; }
        string PreferencesAutoSaveTimeTips { get; }
        // string PreferencesHelpDoc { get; }


        //**********  Commom *********
        string Select { get; }
        string SelectFile { get; }
        string SelectFolder { get; }
        string TipsTitle { get; }
        string TipsConfirm { get; }
        string TipsCancel { get; }
        string CompilingTips { get; }
        string Disable { get; }
        string Locked { get; }
        string Save { get; }
        string Rename { get; }

        //**********  Header *********
        string HeaderLastSaveTime { get; }
        string HeaderSelectAsset { get; }
        string OpenPreferencesTips { get; }
        string SelectAssetTips { get; }
        string OpenMagnetSnappingTips { get; }
        string NewAssetTips { get; }
        string BackMenuTips { get; }
        string PlayLoopTips { get; }
        string PlayForwardTips { get; }
        string StepForwardTips { get; }
        string PauseTips { get; }
        string PlayTips { get; }
        string StopTips { get; }
        string StepBackwardTips { get; }

        string FirstFrame { get; }

        //**********  Group Menu *********
        string MenuAddTrack { get; }
        string MenuPasteTrack { get; }
        string GroupAdd { get; }
        string GroupDisable { get; }
        string GroupLocked { get; }
        string GroupReplica { get; }
        string GroupDelete { get; }
        string GroupDeleteTips { get; }

        //**********  Track Menu *********
        string TrackDisable { get; }
        string TrackLocked { get; }
        string TrackCopy { get; }
        string TrackCut { get; }
        string TrackDelete { get; }
        string TrackDeleteTips { get; }
        //**********  Clip Menu *********
        string ClearCopy { get; }

        string ClipCopy { get; }
        string ClipCut { get; }
        string ClipDelete { get; }
        string MatchClipLength { get; }
        string MatchPreviousLoop { get; }
        string MatchNextLoop { get; }
        string ClipPaste { get; }
        //**********  Inspector *********
        string NotSelectAsset { get; }
        string InsBaseInfo { get; }
        string OverflowInvalid { get; }
        string EndTimeOverflowInvalid { get; }
        string StartTimeOverflowInvalid { get; }

        string ClipInvalid { get; }
        string EmptyRect { get; }
    }
}
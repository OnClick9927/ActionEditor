namespace ActionEditor
{
    interface ILanguages
    {
        //**********  Welcome *********
        public string Title { get; }
        public string CreateAsset { get; }
        public string SelectAsset { get; }
        public string Seeting { get; }


        //**********  Crate Window *********
        public string CrateAssetType { get; }
        public string CrateAssetName { get; }
        public string CreateAssetFileName { get; }
        public string CreateAssetConfirm { get; }
        //public string CreateAssetReset { get; }
        public string CreateAssetTipsNameNull { get; }
        public string CreateAssetTipsRepetitive { get; }


        //**********  Preferences Window *********
        public string PreferencesTitle { get; }
        public string PreferencesTimeStepMode { get; }
        public string PreferencesSnapInterval { get; }
        public string PreferencesFrameRate { get; }
        public string PreferencesMagnetSnapping { get; }
        public string PreferencesMagnetSnappingTips { get; }
        //public string PreferencesScrollWheelZooms { get; }
        public string PreferencesScrollWheelZoomsTips { get; }
        public string PreferencesSavePath { get; }
        public string PreferencesSavePathTips { get; }
        public string PreferencesAutoSaveTime { get; }
        public string PreferencesAutoSaveTimeTips { get; }
        //public string PreferencesHelpDoc { get; }


        //**********  Commom *********
        public string Select { get; }
        public string SelectFile { get; }
        public string SelectFolder { get; }
        public string TipsTitle { get; }
        public string TipsConfirm { get; }
        public string TipsCancel { get; }
        public string CompilingTips { get; }
        public string Disable { get; }
        public string Locked { get; }
        public string Save { get; }
        public string Rename { get; }

        //**********  Header *********
        public string HeaderLastSaveTime { get; }
        public string HeaderSelectAsset { get; }
        public string OpenPreferencesTips { get; }
        public string SelectAssetTips { get; }
        public string OpenMagnetSnappingTips { get; }
        public string NewAssetTips { get; }
        public string BackMenuTips { get; }
        public string PlayLoopTips { get; }
        public string PlayForwardTips { get; }
        public string StepForwardTips { get; }
        public string PauseTips { get; }
        public string PlayTips { get; }
        public string StopTips { get; }
        public string StepBackwardTips { get; }

        public string FirstFrame { get; }

        //**********  Group Menu *********
        public string MenuAddTrack { get; }
        public string MenuPasteTrack { get; }
        public string GroupAdd { get; }
        public string GroupDisable { get; }
        public string GroupLocked { get; }
        public string GroupReplica { get; }
        public string GroupDelete { get; }
        public string GroupDeleteTips { get; }

        //**********  Track Menu *********
        public string TrackDisable { get; }
        public string TrackLocked { get; }
        public string TrackCopy { get; }
        public string TrackCut { get; }
        public string TrackDelete { get; }
        public string TrackDeleteTips { get; }
        //**********  Clip Menu *********
        public string ClearCopy { get; }

        public string ClipCopy { get; }
        public string ClipCut { get; }
        public string ClipDelete { get; }
        public string MatchClipLength { get; }
        public string MatchPreviousLoop { get; }
        public string MatchNextLoop { get; }
        public string ClipPaste { get; }
        //**********  Inspector *********
        public string NotSelectAsset { get; }
        public string InsBaseInfo { get; }
        public string OverflowInvalid { get; }
        public string EndTimeOverflowInvalid { get; }
        public string StartTimeOverflowInvalid { get; }

        public string ClipInvalid { get; }
        string EmptyRect { get;}
    }
}